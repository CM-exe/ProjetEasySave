namespace ProjetEasySave.Model
{
    public class SaveTask
    {
        // Attributes
        private ISaveStrategy _saveStrategy;
        private SaveSpace _saveSpace;
        private SaveTaskState _state;

        public event Action<int, string>? ProgressUpdated;

        // Constructor
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

        // Methods
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

        public SaveTaskState onSaveTaskStateUpdated(ISaveStrategy strategy)
        {
            SaveTaskState newState = strategy.getState();
            setState(newState);
            return newState;
        }

        private SaveTaskState setState(SaveTaskState state)
        {
            _state = state;
            // Notify the SaveSpace of the state change
            _saveSpace.onSaveTaskStateChanged(this, state);
            return _state;

        }

        // Getters

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

        public string getCompleteSavePath()
        {
            if (_saveStrategy is DifferentialSave differentialSave)
            {
                return differentialSave.getCompleteSavePath();
            }
            return string.Empty;
        }

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
