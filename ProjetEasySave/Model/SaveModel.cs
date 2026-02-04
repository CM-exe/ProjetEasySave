namespace ProjetEasySave.Model
{
    public class SaveModel
    {
        // Attributes
        private List<SaveSpace> _saveSpaces;
        private readonly string _configPath;

        // Constructor
        public SaveModel()
        {
            _saveSpaces = new List<SaveSpace>();
            _configPath = Path.Combine(AppContext.BaseDirectory, "../../../config_models.json");
        }

        // Destructor
        ~SaveModel()
        {
            saveToConfig();
        }

        // Methods
        public bool addSaveSpace(string name, string sourcePath, string destinationPath, string typeSave)
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
                var newSaveSpace = new SaveSpace(name, sourcePath, destinationPath, typeSave);
                _saveSpaces.Add(newSaveSpace);
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
                return true;
            }
            return false;
        }

        public List<SaveSpace> getSaveSpaces()
        {
            return _saveSpaces;
        }

        public bool loadSaveSpaces(string jsonConfigPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonConfigPath) || !File.Exists(jsonConfigPath))
                {
                    return false;
                }
                string jsonContent = File.ReadAllText(jsonConfigPath);
                var saveSpacesFromFile = System.Text.Json.JsonSerializer.Deserialize<List<SaveSpace>>(jsonContent);
                if (saveSpacesFromFile != null)
                {
                    _saveSpaces = saveSpacesFromFile;
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool updateSaveSpace(string name, string newSourcePath, string newDestinationPath, string newTypeSave)
        {
            var saveSpaceToUpdate = _saveSpaces.FirstOrDefault(s => s.getName() == name);
            if (saveSpaceToUpdate != null)
            {
                saveSpaceToUpdate.setSourcePath(newSourcePath);
                saveSpaceToUpdate.setDestinationPath(newDestinationPath);
                saveSpaceToUpdate.setTypeSave(newTypeSave);
                return true;
            }
            return false;
        }

        public bool startSave(string name)
        {
            var saveSpaceToStart = _saveSpaces.FirstOrDefault(s => s.getName() == name);
            if (saveSpaceToStart != null)
            {
                return saveSpaceToStart.executeSave();
            }
            return false;
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

                var jsonContent = System.Text.Json.JsonSerializer.Serialize(_saveSpaces);
                File.WriteAllText(_configPath, jsonContent);
            }
            catch (Exception)
            {
                // Ignore exceptions during finalization
            }
        }
    }
}
