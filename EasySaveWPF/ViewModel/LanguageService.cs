using ProjetEasySave.Utils;
using System.IO;
using System.Text.Json;

namespace ProjetEasySave.ViewModel
{
    /// <summary>
    /// Provides internationalization and translation services for the EasySave application.
    /// This class implements the Singleton pattern to ensure a single, thread-safe instance manages all translations.
    /// </summary>
    public class LanguageService
    {
        // Attributes
        private Config config = Config.Instance; // Load config
        private string _currentLanguage;
        private Dictionary<string, Dictionary<string, string>> _translations;
        // Making it singleton
        private static LanguageService _instance = null;
        private static readonly object _lockSingleton = new object();

        /// <summary>
        /// Prevents a default instance of the <see cref="LanguageService"/> class from being created.
        /// Initializes the current language from the application configuration and loads the translation dictionary from a local JSON file.
        /// </summary>
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

        /// <summary>
        /// Retrieves the thread-safe singleton instance of the <see cref="LanguageService"/>.
        /// </summary>
        /// <returns>The single active instance of the translation service.</returns>
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

        /// <summary>
        /// Changes the current application language if the specified language code exists in the loaded translation dictionary.
        /// It also updates the persistent configuration file with the new selection.
        /// </summary>
        /// <param name="languageCode">The new language code to apply (e.g., "en", "fr").</param>
        public void setLanguage(string languageCode)
        {
            if (_translations.ContainsKey(languageCode))
            {
                _currentLanguage = languageCode;
                config.setLanguage(languageCode); // Save the new language in the config file
            }
        }

        /// <summary>
        /// Retrieves the currently active language code.
        /// </summary>
        /// <returns>A string representing the current language code.</returns>
        public string getLanguage()
        {
            return _currentLanguage;
        }

        /// <summary>
        /// Translates a given key into the currently selected language.
        /// </summary>
        /// <param name="key">The string key corresponding to the desired text.</param>
        /// <returns>The translated string. If the key or translation is not found, the original key is returned as a fallback.</returns>
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