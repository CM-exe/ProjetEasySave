namespace ProjetEasySave.Model
{
    public class SaveTask
    {
        // Attributes
        private ISaveStrategy _saveStrategy;
        private SaveSpace _saveSpace;
        private SaveTaskState _state;

        // Constructor
        public SaveTask(string strategy, SaveSpace space)
        {
            switch (strategy.ToLower())
            {
                case "complete":
                    _saveStrategy = new CompleteSave();
                    break;
                case "differential":
                    _saveStrategy = new DifferentialSave();
                    break;
                default:
                    throw new ArgumentException("Invalid save strategy type");
            }
            _saveSpace = space;
        }

        // Methods
        public bool save(string sourceFolder, string destinationFolder)
        {
            this.setState(SaveTaskState.RUNNING);
            bool well_executed = _saveStrategy.doSave(sourceFolder, destinationFolder);
            this.setState(well_executed ? SaveTaskState.COMPLETED : SaveTaskState.FAILED);
            return well_executed;
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
    }
}
