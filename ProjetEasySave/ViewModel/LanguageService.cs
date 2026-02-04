using System.Text.Json;

namespace ProjetEasySave.ViewModel
{
    public class LanguageService
    {
        // Attributes
        private string _currentLanguage;
        private Dictionary<string, Dictionary<string, string>> _translations;

        // Constructor
        public LanguageService()
        {
            _currentLanguage = "en";
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

        // Methods
        public void SetLanguage(string languageCode)
        {
            if (_translations.ContainsKey(languageCode))
            {
                _currentLanguage = languageCode;
            }
        }

        public string GetLanguage()
        {
            return _currentLanguage;
        }

        public string Translate(string key)
        {
            if (_translations.ContainsKey(_currentLanguage) && _translations[_currentLanguage].ContainsKey(key))
            {
                return _translations[_currentLanguage][key];
            }
            return key; // Return the key itself if translation not found
        }
    }
}
