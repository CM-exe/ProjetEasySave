namespace ProjetEasySave.Model
{
    public class SaveTask
    {
        // Attributes
        private ISaveStrategy _saveStrategy;
        private SaveSpace _saveSpace;
        private SaveTaskState _state;

        // Constructor
        public SaveTask(string strategy, SaveSpace space, string completeFolder = "")
        {
            switch (strategy.ToLower())
            {
                case "complete":
                    _saveStrategy = new CompleteSave(this);
                    break;
                case "differential":
                    if (string.IsNullOrWhiteSpace(completeFolder))
                    {
                        throw new ArgumentException("Complete folder path must be provided for differential save strategy");
                    }
                    _saveStrategy = new DifferentialSave(this,completeFolder);
                    break;
                default:
                    throw new ArgumentException("Invalid save strategy type");
            }
            _saveSpace = space;
        }

        // Methods
        public bool save(string sourceFolder, string destinationFolder)
        {
            bool well_executed = _saveStrategy.doSave(sourceFolder, destinationFolder);
            return well_executed;
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
            _saveSpace.onSaveTaskStateChanged(this);
            return _state;

        }

        // Getters
        public SaveTaskState getState()
        {
            return _state;
        }

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

        public Task<bool> saveAsync(string sourceFolder, string destinationFolder)
        {
            return Task.Run(() =>
            {
                return save(sourceFolder, destinationFolder);
            });
        }

    }
}
