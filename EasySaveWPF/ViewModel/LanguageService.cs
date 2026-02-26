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
            _translations = new Translations().TranslationsData; // Load default translations
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

    // The Json structure containing the translations to avoid path issues
    public class Translations
    {
        public Dictionary<string, Dictionary<string, string>> TranslationsData { get; set; }

        public Translations()
        {
            TranslationsData = new Dictionary<string, Dictionary<string, string>>();

            // English translations
            TranslationsData["en"] = new Dictionary<string, string>
            {
                {"AddSaveSpace", "Add" },
                {"RemoveSaveSpace", "Remove"},
                {"StartSave", "Start backup"},
                {"ListSaveSpaces", "List save spaces"},
                {"ChangeLanguage", "Change language"},
                {"Exit", "Exit"},
                {"InvalidChoice", "Invalid choice."},
                {"Name", "Name"},
                {"SourcePath", "Source path"},
                {"DestinationPath", "Destination path"},
                {"SaveType", "Save type"},
                {"SaveSpaceAdded", "Save space added."},
                {"SaveSpaceAddFailed", "Failed to add save space."},
                {"SaveSpaceRemoved", "Save space removed."},
                {"SaveSpaceRemoveFailed", "Failed to remove save space."},
                {"SaveStarted", "Backup started."},
                {"SaveStartFailed", "Failed to start backup."},
                {"SaveSpacesTitle", "Save spaces"},
                {"NoSaveSpaces", "No save spaces."},
                {"InvalidSaveSpaceID", "Invalid save spaces."},
                {"LanguageCodePrompt", "Language code (fr/en)"},
                {"Language", "Language"},
                {"SaveSpacesState", "Save spaces state"},
                {"PressEnter", "Press Enter to return to menu..."},
                {"PressEnterPause", "Press Enter to continue..."},
                {"CompleteSavePath", "Complete save path"},
                {"PriorityExtensions", "Priority file extensions"},
                {"SaveCompleted", "Backup completed."},
                {"SaveFailed", "Failed to backup."},
                {"UsageCommandExemple", "EasySave\nusage: <exe_file> {save_space_id_start|type_of_slide|save_space_id_to}\n  options :\n    save_space_id_start: Index of the first save space to save\n    type_of_slide: Either \";\" to save only save_space_id_start and save_space_id_to ; or \"-\" to save the save spaces ranged between them\n    save_space_id_to: Index of the last save space to save\n\nexemples :\n  EasySave.exe 1-3        # Save save spaces 1, 2, 3\n  EasySave.exe 1;3        # Save save spaces 1, 3\n  EasySave.exe 1          # Error (use 1-1 or 1;1)"},
                {"CurrentLanguage", "Current language"},
                {"EditSaveSpace", "Edit a save space"},
                {"textAppDescription", "Save spaces management"},
                {"textWorkspace", "Save spaces"},
                {"textList", "(List)"},
                {"Source", "Source"},
                {"Destination", "Destination"},
                {"ChangeLogsFormat", "Change logs format"},
                {"ErrorBusinessSoftwareRunning", "Backup cancelled: Business software is running."},
                {"State", "State"},
                {"LogsFormatPrompt", "Logs format (xml/json)"},
                {"LogsFormat", "Logs format"},
                {"CurrentLogsFormat", "Current logs format"},
                {"Ok", "OK"},
                {"Cancel", "Cancel"},
                {"MaxSize", "File size threshold"},
                {"Config", "Configuration"},
                {"CurrentFileLabel", "Current file:"},
                {"PausedSuffix", " (paused)"},
                {"StoppedSuffix", " (stopped)"},
                {"SaveCompletedMsg", "Backup completed successfully"},
                {"SaveStopped", "Backup has been stopped."},
                {"SaveInProgressTitle", "Backup in progress"},
                {"PausePendingSuffix", " (pausing...)"},
                {"Close", "Close"},
                {"Enabled", "Enabled"},
                {"Disabled", "Disabled"},
                {"LogsOnServer", "Logs sent to centralized log server"},
                {"LogsOnLocal", "Logs saved locally"},
                {"ServerIp", "Server ip of the log server"},
                {"ServerPort", "Server port of the log server"},
                {"ReconnectToServer", "Trying to reconnect to the log server..."},
                {"Connected", "Connected"},
                {"Disconnected", "Déconnected"},
                {"Connecting", "Connecting..."},
                {"BusinessSoftware", "Business software"},
                {"StartMultipleSavesConfirmation", "You are about to start multiple backups. Are you sure you want to continue ?"}
            };

            // French translations
            TranslationsData["fr"] = new Dictionary<string, string>
            {
                {"AddSaveSpace", "Ajouter"},
                {"RemoveSaveSpace", "Supprimer"},
                {"StartSave", "Démarrer la sauvegarde"},
                {"ListSaveSpaces", "Lister les espaces de sauvegarde"},
                {"ChangeLanguage", "Changer la langue"},
                {"Exit", "Quitter"},
                {"InvalidChoice", "Choix invalide."},
                {"Name", "Nom"},
                {"SourcePath", "Chemin source"},
                {"DestinationPath", "Chemin destination"},
                {"SaveType", "Type de sauvegarde"},
                {"SaveSpaceAdded", "Espace de sauvegarde ajouté."},
                {"SaveSpaceAddFailed", "Échec de l'ajout de l'espace de sauvegarde."},
                {"SaveSpaceRemoved", "Espace de sauvegarde supprimé."},
                {"SaveSpaceRemoveFailed", "Échec de la suppression de l'espace de sauvegarde."},
                {"SaveStarted", "Sauvegarde démarrée."},
                {"SaveStartFailed", "Échec du démarrage de la sauvegarde."},
                {"SaveSpacesTitle", "Espaces de sauvegarde"},
                {"NoSaveSpaces", "Aucun espace de sauvegarde."},
                {"InvalidSaveSpaceID", "Numéro d'espace de travail invalide."},
                {"LanguageCodePrompt", "Code de langue (fr/en)"},
                {"Language", "Langue"},
                {"SaveSpacesState", "État des espaces de sauvegarde"},
                {"PressEnter", "Appuyez sur Entrée pour revenir au menu..."},
                {"PressEnterPause", "Appuyez sur Entrée pour continuer..."},
                {"CompleteSavePath", "Chemin de la sauvegarde complète"},
                {"PriorityExtensions", "Extension de fichiers prioritaires"},
                {"SaveCompleted", "Sauvegarde terminée."},
                {"SaveFailed", "Échec de la sauvegarde."},
                {"UsageCommandExemple", "EasySave\nusage: <exe_file> {save_space_id_start|type_of_slide|save_space_id_to}\n  options :\n    save_space_id_start: Index of the first save space to save\n    type_of_slide: Either \";\" to save only save_space_id_start and save_space_id_to ; or \"-\" to save the save spaces ranged between them\n    save_space_id_to: Index of the last save space to save\n\nexemples :\n  EasySave.exe 1-3        # Save save spaces 1, 2, 3\n  EasySave.exe 1;3        # Save save spaces 1, 3\n  EasySave.exe 1          # Error (use 1-1 or 1;1)"},
                {"CurrentLanguage", "Langue actuelle"},
                {"EditSaveSpace", "Modifier"},
                {"textAppDescription", "Gestion des espaces de sauvegardes"},
                {"textWorkspace", "Espaces de sauvegardes"},
                {"textList", "(Liste)"},
                {"Source", "Source"},
                {"Destination", "Destination"},
                {"ChangeLogsFormat", "Changer le format des logs"},
                {"ErrorBusinessSoftwareRunning", "Sauvegarde annulée : Le logiciel métier est en cours d'exécution."},
                {"State", "État"},
                {"LogsFormatPrompt", "Format des logs (xml/json)"},
                {"LogsFormat", "Format des logs"},
                {"CurrentLogsFormat", "Format des logs actuel"},
                {"Ok", "OK"},
                {"Cancel", "Annuler"},
                {"MaxSize", "Seuil de tailles des fichiers"},
                {"Config", "Configuration"},
                {"CurrentFileLabel", "Fichier en cours :"},
                {"PausedSuffix", " (en pause)"},
                {"StoppedSuffix", " (arrêtée)"},
                {"SaveCompletedMsg", "Sauvegarde terminée avec succès"},
                {"SaveStopped", "La sauvegarde a été arrêtée."},
                {"SaveInProgressTitle", "Sauvegarde en cours"},
                {"PausePendingSuffix", " (pause en attente...)"},
                {"Close", "Fermer"},
                {"Enabled", "Activé"},
                {"Disabled", "Désactivé"},
                {"LogsOnServer", "Logs envoyés au serveur de log centralisé"},
                {"LogsOnLocal", "Logs enregistrés localement"},
                {"ServerIp", "Ip du serveur de logs"},
                {"ServerPort", "Port du serveur de logs"},
                {"ReconnectToServer", "Essayer de se reconnecter au serveur de logs..."},
                {"Connected", "Connecté"},
                {"Disconnected", "Déconnecté"},
                {"Connecting", "Connexion en cours..."},
                {"BusinessSoftware", "Logiciel métier"},
                {"StartMultipleSavesConfirmation", "Vous êtes sur le point de démarrer plusieurs sauvegardes. Êtes-vous sûr de vouloir continuer ?" }
            };
        }
    }
}
