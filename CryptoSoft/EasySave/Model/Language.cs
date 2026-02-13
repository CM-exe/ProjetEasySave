using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EasySave.Model;

public interface ILanguage {
    // Interface for Language class
    /// <summary>
    /// Dictionary containing the translations for the language
    /// </summary>
    public Dictionary<string, string> Translations { get; set; }
    /// <summary>
    /// Current language
    /// </summary>
    /// <param name="language"></param>
    void SetLanguage(string language);
    /// <summary>
    /// Get the current language
    /// </summary>
    /// <returns></returns>
    string GetLanguage();
    /// <summary>
    /// Load the language from the file
    /// </summary>
    void Load();
    /// <summary>
    /// Returns the language available in the application
    /// </summary>
    List<string> GetAvailableLanguages();

    event LanguageChangedEventHandler LanguageChanged;
}

public class Language : ILanguage {
    // Singleton instance of Language
    public static Language Instance { get; } = new Language();
    // Private constructor to prevent instantiation from outside
    private Language() { }
    public Dictionary<string, string> Translations { get; set; } = [];


    // Initialize the event with an empty delegate to avoid null issues  
    public event LanguageChangedEventHandler LanguageChanged = delegate { };

    public void SetLanguage(string _language) {
        Configuration.Instance!.Language = _language;
        this.Load();
        this.OnLanguageChanged();
    }

    public string GetLanguage() {
        return Configuration.Instance!.Language;
    }

    public void Load() {
        // Implementation for loading language data
        // Load the language from the file
        //Extract the language from the json file in the path "Resources/Language/{language}.json"

        // Deserialize the json file into a Dictionary<string, string>
        // and assign it to the Traductions property

        if (!File.Exists($"Resources/Language/{Configuration.Instance!.Language}.json")) {
            if (Configuration.Instance!.Language == IConfiguration.DEFAULT_LANGUAGE) {
                throw new FileNotFoundException($"Language file not found: Resources/Language/{Configuration.Instance!.Language}.json");
            } else {
                // If the file does not exist, set the language to the default language
                Configuration.Instance!.Language = IConfiguration.DEFAULT_LANGUAGE;
                // Load the default language
                this.Load();
                return;
            }
        }

        string json = File.ReadAllText($"Resources/Language/{Configuration.Instance!.Language}.json");
        this.Translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json)!;
    }

    public List<string> GetAvailableLanguages() {
        // Get the list of available languages
        // Get all the files in the Resources/Language directory
        // and return the list of languages without the .json extension
        string[] files = Directory.GetFiles("Resources/Language", "*.json");
        List<string> languages = [];
        foreach (string file in files) {
            languages.Add(Path.GetFileNameWithoutExtension(file));
        }
        return languages;
    }

    protected virtual void OnLanguageChanged() {
        LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(Configuration.Instance!.Language));
    }
}
