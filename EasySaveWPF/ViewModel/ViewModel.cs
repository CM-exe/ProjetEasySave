using ProjetEasySave.Model;
using ProjetEasySave.Utils;

namespace ProjetEasySave.ViewModel
{
    public class ViewModel
    {
        // Attributes
        private SaveModel _model;
        private LanguageService _languageService;
        private readonly Logger logger = Logger.getInstance(); // Load logger

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

        public void setLogsFormat(string format)
        {
            logger.setLogsFormat(format);
        }

        public string getLogsFormat()
        {
            return logger.getLogsFormat();
        }

        public string translate(string key)
        {
            return _languageService.translate(key);
        }
    }
}
