using ProjetEasySave.Model;
using ProjetEasySave.Utils;

namespace ProjetEasySave.ViewModel
{
    public class ViewModel
    {
        // Attributes
        private SaveModel _model;
        private LanguageService _languageService;

        // Constructor
        public ViewModel()
        {
            _model = new SaveModel();
            _languageService = new LanguageService();
        }

        // Methods
        public bool addSaveSpace(string name, string sourcePath, string destinationPath, string typeSave, string completeSavePath = "")
        {
            return _model.addSaveSpace(name, sourcePath, destinationPath, typeSave, completeSavePath);
        }

        public bool removeSaveSpace(string name)
        {
            return _model.removeSaveSpace(name);
        }

        public bool startSave(string name)
        {
            return _model.startSave(name);
        }

        public List<SaveSpace> getSaveSpaces()
        {
            return _model.getSaveSpaces();
        }

        public void setLanguage(string languageCode)
        {
            _languageService.setLanguage(languageCode);
        }

        public string getLanguage()
        {
            return _languageService.getLanguage();
        }

        public string translate(string key)
        {
            return _languageService.translate(key);
        }

        public bool setLogFormat(string format)
        {
            // Try to parse or match the string to the LogFormat enum
            if (format == "JSON")
            {
                Logger.getInstance().CurrentFormat = LogFormat.Json;
                return true;
            }
            else if (format == "XML")
            {
                Logger.getInstance().CurrentFormat = LogFormat.Xml;
                return true;
            }

            // Return false if the input was invalid
            return false;
        }
    }
}
