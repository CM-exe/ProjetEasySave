using ProjetEasySave.Utils;
using System.Text.Json.Serialization;
using EasyLog;

namespace ProjetEasySave.Model
{
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

		public event Action<int, string>? ProgressChanged;

		private SemaphoreSlim _bigFileSemaphore;

        // Constructor
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

        // EventHandler SaveTaskStateChanged for the view to update the UI in real-time
        public event EventHandler? SaveTaskStateChanged;


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

		public void Play()
		{
			_pauseEvent.Set();
		}

		public void Pause()
		{
			_pauseEvent.Reset();
		}

		public void Stop()
		{
			onSaveTaskStateChanged(_saveTasks[0], SaveTaskState.STOPPED);

			SaveTaskStateChanged?.Invoke(this, EventArgs.Empty);

			_cts.Cancel();
		}

		// Getters
		public string getName()
        {
            return _name;
        }

        public string getSourcePath()
        {
            return _sourcePath;
        }
        public string getDestinationPath()
        {
            return _destinationPath;
        }

        public List<string> getPriorityExt()
        {
            return _priorityExt;
        }

        public string getTypeSave()
        {
            if (_saveTasks.Count > 0)
            {
                return _saveTasks[0].getStrategyType();
            }
            return string.Empty;
        }

        public List<SaveTaskState> getTaskStates()
        {
            return _taskStates.Values.ToList();
        }

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
        public void setName(string name)
        {
            _name = name;
        }

        public void setSourcePath(string sourcePath)
        {
            _sourcePath = sourcePath;
        }

        public void setDestinationPath(string destinationPath)
        {
            _destinationPath = destinationPath;
        }

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
        public void setPriorityExt(string priorityExt)
        {
            _priorityExt = priorityExt.Split(',').Select(ext => ext.Trim()).ToList();
        }
    }
}
