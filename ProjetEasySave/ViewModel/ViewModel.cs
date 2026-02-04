using ProjetEasySave.Model;

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
        public bool addSaveSpace(string name, string sourcePath, string destinationPath, string typeSave)
        {
            return _model.addSaveSpace(name, sourcePath, destinationPath, typeSave);
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
    }
}
