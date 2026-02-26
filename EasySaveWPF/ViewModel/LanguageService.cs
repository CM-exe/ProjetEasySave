using ProjetEasySave.Utils;
using System.IO;
using System.Text.Json;

namespace ProjetEasySave.ViewModel
{
    public class LanguageService
    {
        // Attributes
        private Config config = Config.Instance; // Load config
        private string _currentLanguage;
        private Dictionary<string, Dictionary<string, string>> _translations;
        // Making it singleton
        private static LanguageService _instance = null;
        private static readonly object _lockSingleton = new object();

        // Constructor
        private LanguageService()
        {
            _currentLanguage = config.getLanguage();
            _translations = new Dictionary<string, Dictionary<string, string>>();

            var filePath = Path.Combine(AppContext.BaseDirectory, "../../../translations.json");
            if (!File.Exists(filePath))
            {
                return;
            }

            var json = File.ReadAllText(filePath);
            var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
            if (data != null)
            {
                _translations = data;
            }
        }

        public static LanguageService getInstance()
        {
            lock (_lockSingleton)
            {
                if (_instance == null)
                {
                    _instance = new LanguageService();
                }
            return _instance;
            }
        }

        // Methods
        public void setLanguage(string languageCode)
        {
            if (_translations.ContainsKey(languageCode))
            {
                _currentLanguage = languageCode;
                config.setLanguage(languageCode); // Save the new language in the config file
            }
        }

        public string getLanguage()
        {
            return _currentLanguage;
        }

        public string translate(string key)
        {
            if (_translations.ContainsKey(_currentLanguage) && _translations[_currentLanguage].ContainsKey(key))
            {
                return _translations[_currentLanguage][key];
            }
            return key; // Return the key itself if translation not found
        }
    }
}
