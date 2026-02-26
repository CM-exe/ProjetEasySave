using ProjetEasySave.Utils;
using System.Text.Json.Serialization;
using EasyLog;

namespace ProjetEasySave.Model
{
    /// <summary>
    /// Represents a configured backup job, managing its properties, associated tasks, and execution state.
    /// Acts as an observer to track changes from its underlying <see cref="SaveTask"/> instances.
    /// </summary>
    public class SaveSpace : SaveTaskObserver
    {
        // Attributes
        [JsonInclude]
        private string _name;
        [JsonInclude]
        private string _sourcePath;
        [JsonInclude]
        private string _destinationPath;
        [JsonInclude]
        private List<string> _saveTaskStrategies; // For serialization purposes only
        [JsonInclude]
        private List<string> _saveTaskCompleteSavePaths; // For serialization purposes only
        [JsonInclude]
        private List<string> _priorityExt; // For serialization purposes only
        [JsonIgnore]
        private List<SaveTask> _saveTasks;
        [JsonIgnore]
        private Dictionary<SaveTask, SaveTaskState> _taskStates;
        [JsonIgnore]
        private Logger logger = Logger.getInstance(Config.Instance);
		[JsonIgnore]
		private CancellationTokenSource? _cts;
		[JsonIgnore]
		private ManualResetEventSlim? _pauseEvent;

        /// <summary>
        /// Event triggered when the completion percentage or the current processing file changes.
        /// </summary>
		public event Action<int, string>? ProgressChanged;

		private SemaphoreSlim _bigFileSemaphore;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveSpace"/> class.
        /// </summary>
        /// <param name="name">The designated name of the backup job.</param>
        /// <param name="sourcePath">The absolute path of the directory to back up.</param>
        /// <param name="destinationPath">The absolute path where the backup will be saved.</param>
        /// <param name="typeSave">The strategy type for the backup ("complete" or "differential").</param>
        /// <param name="priorityExt">A list of file extensions that should be processed first.</param>
        /// <param name="bigFileSemaphore">A semaphore used to control concurrent large file transfers.</param>
        /// <param name="completeSavePath">The reference path to a full backup (required if using a differential strategy).</param>
        /// <exception cref="ArgumentException">Thrown when an unsupported save strategy type is provided.</exception>
        public SaveSpace(
            string name, 
            string sourcePath, 
            string destinationPath, 
            string typeSave, 
            List<string> priorityExt, 
            SemaphoreSlim bigFileSemaphore, 
            string completeSavePath = ""
            )
        {
            _name = name;
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
			_cts = new CancellationTokenSource();
			_pauseEvent = new ManualResetEventSlim(true);
			// Initialize save tasks based on the typeSave parameter
			_saveTasks = new List<SaveTask>();
            switch (typeSave.ToLower())
            {
                case "complete":
                    _saveTasks.Add(new SaveTask("complete", this, bigFileSemaphore));
                    break;
                case "differential":
                    _saveTasks.Add(new SaveTask("differential", this, bigFileSemaphore, completeSavePath));
                    break;
                default:
                    throw new ArgumentException("Invalid save strategy type");
            }

            // Store the strategy type for serialization
            _saveTaskStrategies = new List<string>();
            _saveTaskStrategies.Add(typeSave.ToLower());

            // Store the complete save path for serialization (if applicable)
            _saveTaskCompleteSavePaths = new List<string>();
            _saveTaskCompleteSavePaths.Add(completeSavePath);

            // Initialize priority file extensions
            _priorityExt = priorityExt;

            // Initialize task states
            _taskStates = new Dictionary<SaveTask, SaveTaskState>();
            foreach (var task in _saveTasks)
            {
                _taskStates[task] = SaveTaskState.PENDING; // Initial state
            }

            // Set the static semaphore for big file handling
            _bigFileSemaphore = bigFileSemaphore;
        }

        // Methods
        /// <summary>
        /// Handles state change notifications emitted by the underlying <see cref="SaveTask"/> instances.
        /// Logs the transition and notifies the UI layer.
        /// </summary>
        /// <param name="task">The task that triggered the state change.</param>
        /// <param name="newState">The newly applied state.</param>
        /// <returns>The confirmed new state.</returns>
        public SaveTaskState onSaveTaskStateChanged(SaveTask task, SaveTaskState newState)
        {
			if (!_taskStates.ContainsKey(task))
				return newState;

			// Update the state of the task in the dictionary
			_taskStates[task] = newState;

            // Log the state change
			logger.logRealTime(
				Logger.formatInfoRealTimeMessage(
					_name,
					_sourcePath,
					_destinationPath,
					DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
					newState
				)
			);
            
            // Trigger the event to notify the view of the state change
            SaveTaskStateChanged?.Invoke(this, EventArgs.Empty);

            return newState;
        }

        /// <summary>
        /// Event triggered whenever the state of the save space transitions (e.g., from RUNNING to PAUSED).
        /// </summary>
        public event EventHandler? SaveTaskStateChanged;

        /// <summary>
        /// Asynchronously executes all underlying backup tasks configured for this save space.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. Returns <c>true</c> if completed successfully; otherwise, <c>false</c>.</returns>
        public async Task<bool> executeSaveAsync()
        {
			_cts = new CancellationTokenSource();
			_pauseEvent = new ManualResetEventSlim(true);

            try
            {
                var tasks = _saveTasks.Select(task =>
                    task.saveAsync(
                        _sourcePath,
                        _destinationPath,
                        _priorityExt,
                        _cts.Token,
                        _pauseEvent,
                        (percent, file) =>
                        {
                            ProgressChanged?.Invoke(percent, file);
                        }
                    )
                );

                await Task.WhenAll(tasks);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
	    }

        /// <summary>
        /// Resumes the backup process if it was previously paused.
        /// </summary>
		public void Play()
		{
			_pauseEvent.Set();
		}

        /// <summary>
        /// Suspends the active backup process. The process will block until <see cref="Play"/> is called.
        /// </summary>
		public void Pause()
		{
			_pauseEvent.Reset();
		}

        /// <summary>
        /// Cancels the ongoing backup operation permanently.
        /// </summary>
		public void Stop()
		{
			onSaveTaskStateChanged(_saveTasks[0], SaveTaskState.STOPPED);

			SaveTaskStateChanged?.Invoke(this, EventArgs.Empty);

			_cts.Cancel();
		}

        // Getters

        /// <summary>Gets the designated name of the save space.</summary>
        /// <returns>The name of the save space.</returns>
        public string getName()
        {
            return _name;
        }

        /// <summary>Gets the configured source directory path.</summary>
        /// <returns>The absolute path to the source directory.</returns>
        public string getSourcePath()
        {
            return _sourcePath;
        }

        /// <summary>Gets the configured destination directory path.</summary>
        /// <returns>The absolute path to the destination directory.</returns>
        public string getDestinationPath()
        {
            return _destinationPath;
        }

        /// <summary>Gets the list of priority file extensions.</summary>
        /// <returns>A list of file extension strings.</returns>
        public List<string> getPriorityExt()
        {
            return _priorityExt;
        }

        /// <summary>Gets the defined backup strategy type.</summary>
        /// <returns>The string representation of the backup strategy (e.g., "complete" or "differential").</returns>
        public string getTypeSave()
        {
            if (_saveTasks.Count > 0)
            {
                return _saveTasks[0].getStrategyType();
            }
            return string.Empty;
        }

        /// <summary>Gets the collection of current states for all managed tasks.</summary>
        /// <returns>A list containing the <see cref="SaveTaskState"/> values.</returns>
        public List<SaveTaskState> getTaskStates()
        {
            return _taskStates.Values.ToList();
        }

        /// <summary>Gets the reference path utilized for complete saves when processing a differential backup.</summary>
        /// <returns>The path to the complete backup, or an empty string if not applicable.</returns>
        public string getCompleteSavePath()
        {
            var completeTask = _saveTasks.FirstOrDefault(t => t.getStrategyType() == "differential");
            if (completeTask != null)
            {
                return completeTask.getCompleteSavePath();
            }
            return string.Empty;
        }


        // Setters

        /// <summary>Sets the designated name of the save space.</summary>
        /// <param name="name">The new name to apply.</param>
        public void setName(string name)
        {
            _name = name;
        }

        /// <summary>Sets the source directory path.</summary>
        /// <param name="sourcePath">The new absolute path to the source directory.</param>
        public void setSourcePath(string sourcePath)
        {
            _sourcePath = sourcePath;
        }

        /// <summary>Sets the destination directory path.</summary>
        /// <param name="destinationPath">The new absolute path to the destination directory.</param>
        public void setDestinationPath(string destinationPath)
        {
            _destinationPath = destinationPath;
        }

        /// <summary>
        /// Modifies the backup strategy type and reinitializes the underlying tasks.
        /// </summary>
        /// <param name="typeSave">The new strategy type ("complete" or "differential").</param>
        /// <exception cref="ArgumentException">Thrown when an invalid strategy type is provided.</exception>
        public void setTypeSave(string typeSave)
        {
            _saveTasks.Clear();
            switch (typeSave.ToLower())
            {
                case "complete":
                    _saveTasks.Add(new SaveTask("complete", this, _bigFileSemaphore));
                    break;
                case "differential":
                    _saveTasks.Add(new SaveTask("differential", this, _bigFileSemaphore));
                    break;
                default:
                    throw new ArgumentException("Invalid save strategy type");
            }
            // Reset task states
            _taskStates.Clear();
            foreach (var task in _saveTasks)
            {
                _taskStates[task] = SaveTaskState.PENDING; // Initial state
            }
        }

        /// <summary>
        /// Parses and sets the priority file extensions from a comma-separated string.
        /// </summary>
        /// <param name="priorityExt">A comma-separated string of extensions (e.g., ".txt,.docx").</param>
        public void setPriorityExt(string priorityExt)
        {
            _priorityExt = priorityExt.Split(',').Select(ext => ext.Trim()).ToList();
        }
    }
}