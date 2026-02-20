using ProjetEasySave.Model;
using ProjetEasySave.Utils;
using System.Diagnostics;
using EasyLog;

namespace ProjetEasySave.ViewModel
{
    public class ViewModel
    {
        // Attributes
        private SaveModel _model;
        private LanguageService _languageService;
        private readonly Logger logger = Logger.getInstance(Config.Instance); // Load logger



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

        public async Task<bool> startSave(string name)
        {
            return await _model.startSave(name);
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

        public bool setLogsFormat(string format)
        {
            try
            {
                logger.setLogsFormat(format);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public string getLogsFormat()
        {
            return logger.getLogsFormat();
        }

        public string translate(string key)
        {
            return _languageService.translate(key);
        }


        public long GetMaxFileSize()
        {
            return Config.Instance.getMaxFileSize();
        }

        public void SaveMaxFileSize(string sizeText)
        {
            // On convertit le string en long ici
            if (long.TryParse(sizeText, out long newSize))
            {
                Config.Instance.setMaxFileSize(newSize);
            }
        }

    }
}


