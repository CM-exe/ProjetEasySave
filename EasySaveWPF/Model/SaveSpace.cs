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

        private SemaphoreSlim _bigFileSemaphore;

        // Constructor
        public SaveSpace(string name, string sourcePath, string destinationPath, string typeSave, List<string> priorityExt, SemaphoreSlim bigFileSemaphore, string completeSavePath = "")
        {
            _name = name;
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
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
        public SaveTaskState onSaveTaskStateChanged(SaveTask task)
        {
            // Update the state of the task in the dictionary
            _taskStates[task] = task.getState();
            // Log the state change
            logger.logRealTime(Logger.formatInfoRealTimeMessage(_name, _sourcePath, _destinationPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), task.getState()));
            
            // Trigger the event to notify the view of the state change
            SaveTaskStateChanged?.Invoke(this, EventArgs.Empty);

            return task.getState();
        }

        // EventHandler SaveTaskStateChanged for the view to update the UI in real-time
        public event EventHandler? SaveTaskStateChanged;


        public async Task<bool> executeSaveAsync()
        {
            var tasks = new List<Task<bool>>();

            foreach (var saveTask in _saveTasks)
            {
                tasks.Add(
                    saveTask.saveAsync(_sourcePath, _destinationPath, _priorityExt)
                );
            }

            bool[] results = await Task.WhenAll(tasks);

            return results.All(r => r);
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
