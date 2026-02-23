namespace ProjetEasySave.Model
{
    public class SaveTask
    {
        // Attributes
        private ISaveStrategy _saveStrategy;
        private SaveSpace _saveSpace;
        private SaveTaskState _state;

        private CancellationTokenSource _cts;
        private ManualResetEventSlim _pauseEvent = new(true);

        public event Action<int, string>? ProgressUpdated; // Event for progress updates

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
            _state = SaveTaskState.PENDING;
        }

        public async Task<bool> StartAsync(string sourceFolder, string destinationFolder)
        {
            _cts = new CancellationTokenSource();
            setState(SaveTaskState.RUNNING);

            try
            {
                bool result = await Task.Run(() =>
                    _saveStrategy.doSave(
                        sourceFolder,
                        destinationFolder,
                        _cts.Token,
                        _pauseEvent,
                        reportProgress
                    )
                );

                setState(result ? SaveTaskState.COMPLETED : SaveTaskState.FAILED);
                return result;
            }
            catch (OperationCanceledException)
            {
                setState(SaveTaskState.STOPPED);
                return false;
            }
        }

        public void Pause()
        {
            if (_state == SaveTaskState.RUNNING)
                _pauseEvent.Reset();
        }

        public void Resume()
        {
            if (_state == SaveTaskState.RUNNING)
                _pauseEvent.Set();
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        // Methods
        //public bool save(string sourceFolder, string destinationFolder)
        //{
        //    return _saveStrategy.doSave(sourceFolder, destinationFolder);
        //}

        public bool save(
    string sourceFolder,
    string destinationFolder,
    CancellationToken token,
    ManualResetEventSlim pauseEvent,
    Action<int, string>? progress)
        {
            _saveSpace.updateTaskState(this, SaveTaskState.RUNNING);

            try
            {
                bool ok = _saveStrategy.doSave(
                    sourceFolder,
                    destinationFolder,
                    token,
                    pauseEvent,
                    progress
                );

                _saveSpace.updateTaskState(
                    this,
                    ok ? SaveTaskState.COMPLETED : SaveTaskState.FAILED
                );

                return ok;
            }
            catch (OperationCanceledException)
            {
                _saveSpace.updateTaskState(this, SaveTaskState.STOPPED);
                throw;
            }
        }

        //public Task<bool> saveAsync(string sourceFolder, string destinationFolder)
        //{
        //    return Task.Run(() =>
        //    {
        //        return save(sourceFolder, destinationFolder);
        //    });
        //}

        public Task<bool> saveAsync(
    string sourceFolder,
    string destinationFolder,
    CancellationToken token,
    ManualResetEventSlim pauseEvent,
    Action<int, string>? progress = null)
        {
            return Task.Run(() =>
            {
                return save(
                    sourceFolder,
                    destinationFolder,
                    token,
                    pauseEvent,
                    progress
                );
            }, token);
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
            _saveSpace.updateTaskState(this, state);
            return _state;

        }

        // Progress

        private void reportProgress(int progress, string message)
        {
            ProgressUpdated?.Invoke(progress, message);
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
    }
}
