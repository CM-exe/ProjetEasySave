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
        public bool AddSaveSpace(string name, string sourcePath, string destinationPath, string typeSave)
        {
            return _model.addSaveSpace(name, sourcePath, destinationPath, typeSave);
        }

        public bool RemoveSaveSpace(string name)
        {
            return _model.removeSaveSpace(name);
        }

        public bool StartSave(string name)
        {
            return _model.startSave(name);
        }

        public List<SaveSpace> GetSaveSpaces()
        {
            return _model.getSaveSpaces();
        }

        public void SetLanguage(string languageCode)
        {
            _languageService.SetLanguage(languageCode);
        }

        public string GetLanguage()
        {
            return _languageService.GetLanguage();
        }

        public string Translate(string key)
        {
            return _languageService.Translate(key);
        }
    }
}
