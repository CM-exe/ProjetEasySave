using ProjetEasySave.Utils;
using System.Runtime.Serialization;
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
        [JsonIgnore]
        private List<SaveTask> _saveTasks;
        [JsonIgnore]
        private Dictionary<SaveTask, SaveTaskState> _taskStates;
        [JsonIgnore]
        private Logger logger = Logger.getInstance(Config.Instance);

        // Constructor
        public SaveSpace(string name, string sourcePath, string destinationPath, string typeSave, string completeSavePath = "")
        {
            _name = name;
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
            // Initialize save tasks based on the typeSave parameter
            _saveTasks = new List<SaveTask>();
            switch (typeSave.ToLower())
            {
                case "complete":
                    _saveTasks.Add(new SaveTask("complete", this));
                    break;
                case "differential":
                    _saveTasks.Add(new SaveTask("differential", this, completeSavePath));
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

            // Initialize task states
            _taskStates = new Dictionary<SaveTask, SaveTaskState>();
            foreach (var task in _saveTasks)
            {
                _taskStates[task] = SaveTaskState.PENDING; // Initial state
            }
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


        public bool executeSave(Func<bool> businessSoftwareChecker = null)
        {
            foreach (var task in _saveTasks)
            {
                if (!task.save(_sourcePath, _destinationPath, businessSoftwareChecker))
                {
                    return false; // If any save task fails, return false
                }
            }
            return true; // All save tasks succeeded
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
                    _saveTasks.Add(new SaveTask("complete", this));
                    break;
                case "differential":
                    _saveTasks.Add(new SaveTask("differential", this));
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
    }
}
