using ProjetEasySave.Utils;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

namespace ProjetEasySave.Model
{
    /// <summary>
    /// Serves as the central data model for the application, managing the collection of backup jobs (<see cref="SaveSpace"/>).
    /// </summary>
    /// <remarks>
    /// This class handles the persistence of backup configurations, provides CRUD operations for save spaces, 
    /// and manages application-wide concurrency controls such as big file semaphores and directory locks.
    /// </remarks>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveModel"/> class.
        /// Sets up the big file semaphore and attempts to load existing save spaces from the local configuration file.
        /// </summary>
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

        /// <summary>
        /// Finalizes an instance of the <see cref="SaveModel"/> class.
        /// Ensures that all current save spaces are persisted to the configuration file before the object is destroyed.
        /// </summary>
        ~SaveModel()
        {
            saveToConfig();
        }

        // Serialization/Deserialization constructor

        // Methods
        /// <summary>
        /// Adds a new backup job (Save Space) to the model.
        /// </summary>
        /// <param name="name">The unique name of the backup job.</param>
        /// <param name="sourcePath">The source directory path.</param>
        /// <param name="destinationPath">The destination directory path.</param>
        /// <param name="typeSave">The type of backup ("complete" or "differential").</param>
        /// <param name="priorityExt">A list of extensions that should be processed first.</param>
        /// <param name="completeSavePath">The reference path for differential backups (optional).</param>
        /// <returns><c>true</c> if the save space was successfully added; <c>false</c> if validation failed or it is a duplicate.</returns>
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

        /// <summary>
        /// Removes an existing backup job from the model.
        /// </summary>
        /// <param name="name">The name of the save space to remove.</param>
        /// <returns><c>true</c> if the save space was successfully found and removed; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Retrieves the list of all configured save spaces.
        /// </summary>
        /// <returns>A list containing all current <see cref="SaveSpace"/> objects.</returns>
        public List<SaveSpace> getSaveSpaces()
        {
            return _saveSpaces;
        }

        /// <summary>
        /// Deserializes and loads save spaces from a specified JSON configuration file.
        /// </summary>
        /// <param name="jsonConfigPath">The path to the JSON configuration file.</param>
        /// <param name="dict_lock">The dictionary used to manage directory locks across threads.</param>
        /// <returns><c>true</c> if the configuration was successfully loaded; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Updates the configuration of an existing save space.
        /// </summary>
        /// <param name="name">The name of the save space to modify.</param>
        /// <param name="newSourcePath">The new source directory path.</param>
        /// <param name="newDestinationPath">The new destination directory path.</param>
        /// <param name="newTypeSave">The new type of backup.</param>
        /// <param name="priorityExt">The updated string of priority extensions.</param>
        /// <param name="dict_loc">The global dictionary used for thread locks.</param>
        /// <returns><c>true</c> if the save space was found and updated; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Initiates the backup process for a specific save space.
        /// </summary>
        /// <param name="name">The name of the save space to execute.</param>
        /// <returns>A task representing the asynchronous operation, returning <c>true</c> if successful.</returns>
        public async Task<bool> startSave(string name)
        {
            var saveSpaceToStart = _saveSpaces.FirstOrDefault(s => s.getName() == name);
            if (saveSpaceToStart != null)
            {
                return await saveSpaceToStart.executeSaveAsync();
            }
            return false;
        }

        /// <summary>
        /// Initiates the backup process for a specific save space asynchronously.
        /// </summary>
        /// <param name="name">The name of the save space to execute.</param>
        /// <returns>A task containing the boolean result of the backup operation.</returns>
        public Task<bool> StartSaveAsync(string name)
        {
            var saveSpace = _saveSpaces.FirstOrDefault(s => s.getName() == name);
            return saveSpace?.executeSaveAsync() ?? Task.FromResult(false);
        }

        /// <summary>
        /// Pauses the ongoing backup process for a specified save space.
        /// </summary>
        /// <param name="name">The name of the save space to pause.</param>
        public void PauseSave(string name)
        {
            _saveSpaces.First(s => s.getName() == name).Pause();
        }

        /// <summary>
        /// Resumes a previously paused backup process for a specified save space.
        /// </summary>
        /// <param name="name">The name of the save space to resume.</param>
        public void ResumeSave(string name)
        {
            _saveSpaces.First(s => s.getName() == name).Play();
        }

        /// <summary>
        /// Stops and cancels the ongoing backup process for a specified save space.
        /// </summary>
        /// <param name="name">The name of the save space to stop.</param>
        public void StopSave(string name)
        {
            _saveSpaces.First(s => s.getName() == name).Stop();
        }

        /// <summary>
        /// Subscribes an event handler to listen for progress updates from a specific backup job.
        /// </summary>
        /// <param name="name">The name of the save space emitting the progress events.</param>
        /// <param name="handler">An action delegate accepting the completion percentage and current file name.</param>
        public void SubscribeProgress(string name, Action<int, string> handler)
        {
            _saveSpaces.First(s => s.getName() == name)
                       .ProgressChanged += handler;
        }

        /// <summary>
        /// Serializes the current state of all save spaces and writes it to the local JSON configuration file.
        /// </summary>
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

        /// <summary>
        /// Retrieves the global dictionary used to lock destination directories and prevent concurrent writing conflicts.
        /// </summary>
        /// <returns>A thread-safe <see cref="ConcurrentDictionary{TKey, TValue}"/> mapping paths to lock objects.</returns>
        public static ConcurrentDictionary<string, object> getDictLock()
        {
            return _dict_lock;
        }

        /// <summary>
        /// Regenerates the global dictionary of directory locks based on the current list of save spaces.
        /// Ensures each source and destination path has an associated locking object.
        /// </summary>
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

        /// <summary>
        /// Checks if the configured business software is currently running on the system.
        /// </summary>
        /// <returns><c>true</c> if the business software is active; otherwise, <c>false</c>.</returns>
        public bool isBusinessSoftwareRunning()
        {
            ISaveStrategy tempStrategy = new CompleteSave(null, null);
            return tempStrategy.isBusinessSoftwareRunning();
        }
    }
}