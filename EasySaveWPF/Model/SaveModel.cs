using ProjetEasySave.Utils;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

namespace ProjetEasySave.Model
{
    public class SaveModel
    {
        // Attribute
        [JsonInclude]
        private List<SaveSpace> _saveSpaces;
        // Load config
        private Config _config = Config.Instance;
        private string _configPath;

        // Lock dict
        private static ConcurrentDictionary<string, object> _dict_lock = new ConcurrentDictionary<string, object>();

        // Semaphore for big files to avoid multiple big saves at the same time
        private static SemaphoreSlim _bigFileSemaphore;

        // Constructor
        public SaveModel()
        {
            _saveSpaces = new List<SaveSpace>();
            _configPath = _config.getConfigModelsPath();
            _bigFileSemaphore = new SemaphoreSlim(1, 1);
            // Load existing SaveSpaces from config file if it exists
            if (File.Exists(_configPath))
            {
                loadSaveSpaces(_configPath, _dict_lock);
            }
        }

        // Destructor
        ~SaveModel()
        {
            saveToConfig();
        }

        // Serialization/Deserialization constructor

        // Methods
        public bool addSaveSpace(string name, string sourcePath, string destinationPath, string typeSave, List<string> priorityExt, string completeSavePath = "")
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(name) ||
                    string.IsNullOrWhiteSpace(sourcePath) ||
                    string.IsNullOrWhiteSpace(destinationPath) ||
                    string.IsNullOrWhiteSpace(typeSave))
                {
                    return false;
                }

                // Check for duplicate SaveSpace
                var isDuplicate = _saveSpaces.Any(s =>
                    s.getName() == name);

                if (isDuplicate)
                {
                    return false;
                }

                // Create and add the new SaveSpace
                var newSaveSpace = new SaveSpace(name, sourcePath, destinationPath, typeSave, priorityExt, _bigFileSemaphore, completeSavePath);
                _saveSpaces.Add(newSaveSpace);

                // Update Config
                saveToConfig();

                // Add a lock object for the new SaveSpace to _dict_lock
                updateDictLock();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool removeSaveSpace(string name)
        {
            var saveSpaceToRemove = _saveSpaces.FirstOrDefault(s => s.getName() == name);
            if (saveSpaceToRemove != null)
            {
                _saveSpaces.Remove(saveSpaceToRemove);

                // Update Config
                saveToConfig();

                // Remove the lock object for the removed SaveSpace from _dict_lock
                updateDictLock();

                return true;
            }
            return false;
        }

        public List<SaveSpace> getSaveSpaces()
        {
            return _saveSpaces;
        }

        public bool loadSaveSpaces(string jsonConfigPath, ConcurrentDictionary<string, object> dict_lock)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonConfigPath) || !File.Exists(jsonConfigPath))
                {
                    return false;
                }

                // Get the JSON content from the file
                var json = File.ReadAllText(jsonConfigPath);

                // Go through the JSON content and create manually SaveSpace objects
                var jsonDoc = JsonDocument.Parse(json);
                var spaces = new List<SaveSpace>();
                foreach (var element in jsonDoc.RootElement.GetProperty("_saveSpaces").EnumerateArray())
                {
                    var name = element.GetProperty("_name").GetString() ?? "";
                    var sourcePath = element.GetProperty("_sourcePath").GetString() ?? "";
                    var destinationPath = element.GetProperty("_destinationPath").GetString() ?? "";
                    var typeSave = element.GetProperty("_saveTaskStrategies").EnumerateArray()
                        .Select(e => e.GetString() ?? "")
                        .ToList();
                    var typeSaveValue = typeSave.Count > 0 ? typeSave[0] : "";
                    var completeSavePath = element.GetProperty("_saveTaskCompleteSavePaths").EnumerateArray()
                        .Select(e => e.GetString() ?? "")
                        .ToList();
                    var completeSavePathValue = completeSavePath.Count > 0 ? completeSavePath[0] : "";
                    var priorityExt = element.TryGetProperty("_priorityExt", out var y) && y.ValueKind == JsonValueKind.Array ? y.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() ?? "" : item.ToString()).ToList() : new List<string>();
                    var saveSpace = new SaveSpace(name, sourcePath, destinationPath, typeSaveValue, priorityExt, _bigFileSemaphore, completeSavePathValue);
                    spaces.Add(saveSpace);

                    // Add a lock object for the new SaveSpace to dict_lock
                    updateDictLock();
                }
                _saveSpaces = spaces;

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public bool updateSaveSpace(string name, string newSourcePath, string newDestinationPath, string newTypeSave, string priorityExt, ConcurrentDictionary<string, object> dict_loc)
        {
            var saveSpaceToUpdate = _saveSpaces.FirstOrDefault(s => s.getName() == name);
            if (saveSpaceToUpdate != null)
            {
                // Update Save Space
                saveSpaceToUpdate.setSourcePath(newSourcePath);
                saveSpaceToUpdate.setDestinationPath(newDestinationPath);
                saveSpaceToUpdate.setTypeSave(newTypeSave);
                saveSpaceToUpdate.setPriorityExt(priorityExt);

                // Update Config
                saveToConfig();

                // Update the lock object for the updated SaveSpace in dict_lock
                updateDictLock();

                return true;
            }
            return false;
        }

        public async Task<bool> startSave(string name)
        {
            var saveSpaceToStart = _saveSpaces.FirstOrDefault(s => s.getName() == name);
            if (saveSpaceToStart != null)
            {
                return await saveSpaceToStart.executeSaveAsync();
            }
            return false;
        }

        public Task<bool> StartSaveAsync(string name)
        {
            var saveSpace = _saveSpaces.FirstOrDefault(s => s.getName() == name);
            return saveSpace?.executeSaveAsync() ?? Task.FromResult(false);
        }

        public void PauseSave(string name)
        {
            _saveSpaces.First(s => s.getName() == name).Pause();
        }

        public void ResumeSave(string name)
        {
            _saveSpaces.First(s => s.getName() == name).Play();
        }
        public void StopSave(string name)
        {
            _saveSpaces.First(s => s.getName() == name).Stop();
        }

        public void SubscribeProgress(string name, Action<int, string> handler)
        {
            _saveSpaces.First(s => s.getName() == name)
                       .ProgressChanged += handler;
        }

        // Private method to save the current SaveSpaces to the config file
        private void saveToConfig()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_configPath))
                {
                    return;
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception)
            {
                // Ignore exceptions during finalization
            }
        }

        public static ConcurrentDictionary<string, object> getDictLock()
        {
            return _dict_lock;
        }

        private void updateDictLock()
        {
            ConcurrentDictionary<string, object> temp_dict_lock = new();
            foreach (SaveSpace saveSpace in _saveSpaces)
            {
                temp_dict_lock.GetOrAdd(saveSpace.getSourcePath(), _ => new object());
                temp_dict_lock.GetOrAdd(saveSpace.getDestinationPath(), _ => new object());
            }
            _dict_lock = temp_dict_lock;
        }
    }
}
