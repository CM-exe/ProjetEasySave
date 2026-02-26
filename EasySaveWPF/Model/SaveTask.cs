namespace ProjetEasySave.Model
{
    /// <summary>
    /// Represents an individual execution unit for a backup operation.
    /// This class wraps a specific backup strategy (Complete or Differential) and manages its lifecycle and state.
    /// </summary>
    public class SaveTask
    {
        // Attributes
        private ISaveStrategy _saveStrategy;
        private SaveSpace _saveSpace;
        private SaveTaskState _state;

        /// <summary>
        /// Event triggered when the backup progress updates, providing the completion percentage and the current file name.
        /// </summary>
        public event Action<int, string>? ProgressUpdated;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveTask"/> class, instantiating the appropriate backup strategy.
        /// </summary>
        /// <param name="strategy">The type of strategy to use ("complete" or "differential").</param>
        /// <param name="space">The parent <see cref="SaveSpace"/> that owns this task.</param>
        /// <param name="bigFileSemaphore">A semaphore used to throttle the transfer of large files.</param>
        /// <param name="completeFolder">The reference path to a full backup. Required if the strategy is "differential".</param>
        /// <exception cref="ArgumentException">Thrown when a differential strategy lacks a complete folder path, or if an unknown strategy type is provided.</exception>
        public SaveTask(string strategy, SaveSpace space, SemaphoreSlim bigFileSemaphore, string completeFolder = "")
        {
            switch (strategy.ToLower())
            {
                case "complete":
                    _saveStrategy = new CompleteSave(this, bigFileSemaphore);
                    break;
                case "differential":
                    if (string.IsNullOrWhiteSpace(completeFolder))
                    {
                        throw new ArgumentException("Complete folder path must be provided for differential save strategy");
                    }
                    _saveStrategy = new DifferentialSave(this, bigFileSemaphore, completeFolder);
                    break;
                default:
                    throw new ArgumentException("Invalid save strategy type");
            }
            _saveSpace = space;
            _state = SaveTaskState.PENDING;
        }

        /// <summary>
        /// Synchronously executes the backup operation using the configured strategy.
        /// </summary>
        /// <param name="sourceFolder">The absolute path of the directory to back up.</param>
        /// <param name="destinationFolder">The absolute path where the backup will be saved.</param>
        /// <param name="priorityExt">A list of file extensions that should be processed with higher priority.</param>
        /// <param name="token">A cancellation token to handle explicit stop requests.</param>
        /// <param name="pauseEvent">A manual reset event to manage pausing and resuming the backup process.</param>
        /// <param name="progress">An action delegate to report progress updates back to the UI.</param>
        /// <returns><c>true</c> if the backup completes successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the backup process is explicitly stopped via the cancellation token.</exception>
        public bool save(
            string sourceFolder,
            string destinationFolder,
            List<string> priorityExt,
            CancellationToken token,
            ManualResetEventSlim pauseEvent,
            Action<int, string>? progress)
        {
            _saveSpace.onSaveTaskStateChanged(this, SaveTaskState.RUNNING);

            try
            {
                bool ok = _saveStrategy.doSave(
                    sourceFolder,
                    destinationFolder,
                    priorityExt,
                    token,
                    pauseEvent,
                    progress
                );

                _saveSpace.onSaveTaskStateChanged(
                    this,
                    ok ? SaveTaskState.COMPLETED : SaveTaskState.FAILED
                );

                return ok;
            }
            catch (OperationCanceledException)
            {
                _saveSpace.onSaveTaskStateChanged(this, SaveTaskState.STOPPED);
                throw;
            }
        }

        /// <summary>
        /// Callback method triggered by the underlying <see cref="ISaveStrategy"/> when its internal state changes.
        /// </summary>
        /// <param name="strategy">The strategy reporting the state change.</param>
        /// <returns>The newly applied <see cref="SaveTaskState"/>.</returns>
        public SaveTaskState onSaveTaskStateUpdated(ISaveStrategy strategy)
        {
            SaveTaskState newState = strategy.getState();
            setState(newState);
            return newState;
        }

        /// <summary>
        /// Internally updates the task's state and notifies the parent <see cref="SaveSpace"/>.
        /// </summary>
        /// <param name="state">The new state to apply.</param>
        /// <returns>The updated state.</returns>
        private SaveTaskState setState(SaveTaskState state)
        {
            _state = state;
            // Notify the SaveSpace of the state change
            _saveSpace.onSaveTaskStateChanged(this, state);
            return _state;
        }

        // Getters

        /// <summary>
        /// Identifies the type of strategy currently assigned to this task.
        /// </summary>
        /// <returns>A string representing the strategy type ("complete", "differential", or "unknown").</returns>
        public string getStrategyType()
        {
            if (_saveStrategy is CompleteSave)
            {
                return "complete";
            }
            else if (_saveStrategy is DifferentialSave)
            {
                return "differential";
            }
            else
            {
                return "unknown";
            }
        }

        /// <summary>
        /// Retrieves the reference full backup path used if the current strategy is differential.
        /// </summary>
        /// <returns>The path to the full backup directory, or an empty string if not applicable.</returns>
        public string getCompleteSavePath()
        {
            if (_saveStrategy is DifferentialSave differentialSave)
            {
                return differentialSave.getCompleteSavePath();
            }
            return string.Empty;
        }

        /// <summary>
        /// Asynchronously executes the backup operation using the configured strategy by running it on a background thread.
        /// </summary>
        /// <param name="sourceFolder">The absolute path of the directory to back up.</param>
        /// <param name="destinationFolder">The absolute path where the backup will be saved.</param>
        /// <param name="priorityExt">A list of file extensions that should be processed with higher priority.</param>
        /// <param name="token">A cancellation token to handle explicit stop requests.</param>
        /// <param name="pauseEvent">A manual reset event to manage pausing and resuming the backup process.</param>
        /// <param name="progress">An action delegate to report progress updates back to the UI (optional).</param>
        /// <returns>A task representing the asynchronous save operation, containing a boolean result of success or failure.</returns>
        public Task<bool> saveAsync(
            string sourceFolder,
            string destinationFolder,
            List<string> priorityExt,
            CancellationToken token,
            ManualResetEventSlim pauseEvent,
            Action<int, string>? progress = null)
        {
            return Task.Run(() =>
            {
                return save(
                    sourceFolder,
                    destinationFolder,
                    priorityExt,
                    token,
                    pauseEvent,
                    progress
                );
            }, token);
        }
    }
}