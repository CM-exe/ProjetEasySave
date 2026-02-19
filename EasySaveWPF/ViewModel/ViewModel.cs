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
        private readonly Config config = Config.Instance; // Load config

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

        public bool setLogsFormat(string format)
        {
            try
            {
                logger.setLogsFormat(format);
            }
            catch (Exception e) {
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

        public bool isBusinessSoftwareRunning()
        {
            // Name of the process to look for. 
            // Note: For testing, "CalculatorApp" or "win32calc" depends on the Windows version.
            string businessSoftwareName = config.getBusinessSoftwareName();

            // Fetch all processes matching the specified name
            Process[] processes = Process.GetProcessesByName(businessSoftwareName);

            // Return true if at least one instance is actively running
            return processes.Length > 0;
        }
    }
}
