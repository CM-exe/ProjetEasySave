using ProjetEasySave.Model;
using ProjetEasySave.Utils;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using EasyLog;

namespace ProjetEasySave.ViewModel
{
    /// <summary>
    /// Represents the real-time execution state of a specific backup job.
    /// Implements <see cref="INotifyPropertyChanged"/> to allow seamless data binding with the UI.
    /// </summary>
    public class SaveJobState : INotifyPropertyChanged
    {
        private int _progress;

        /// <summary>
        /// Gets or sets the completion percentage of the backup job.
        /// </summary>
        public int Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        private string _currentFile = string.Empty;

        /// <summary>
        /// Gets or sets the name of the file currently being processed.
        /// </summary>
        public string CurrentFile
        {
            get => _currentFile;
            set { _currentFile = value; OnPropertyChanged(); }
        }

        private bool _isPausePending = false;

        /// <summary>
        /// Gets or sets a value indicating whether a pause has been requested but not yet fully applied.
        /// </summary>
        public bool IsPausePending
        {
            get => _isPausePending;
            set { _isPausePending = value; OnPropertyChanged(); }
        }

        // Added to differentiate between "Waiting to pause" and "Actually paused"
        private bool _isPaused = false;

        /// <summary>
        /// Gets or sets a value indicating whether the backup job is currently fully paused.
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            set { _isPaused = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Occurs when a property value changes, notifying the bound UI elements.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Triggers the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="name">The name of the property that changed. Automatically populated by the compiler.</param>
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    /// <summary>
    /// The central ViewModel for the application, acting as the intermediary between the View layer and the underlying Models.
    /// Manages application state, UI translations, business logic routing, and background monitoring tasks.
    /// </summary>
    public class ViewModel : INotifyPropertyChanged
    {
        // Attributes
        private SaveModel _model;
        private LanguageService _languageService;
        private readonly Logger logger = Logger.getInstance(Config.Instance); // Load logger

        // Translations properties

        /// <summary>Gets the localized label for the current file.</summary>
        public string CurrentFileLabel => translate("CurrentFileLabel");
        /// <summary>Gets the localized suffix applied when a job is fully paused.</summary>
        public string PausedSuffix => translate("PausedSuffix");
        /// <summary>Gets the localized suffix applied when a job is pending a pause.</summary>
        public string PausePendingSuffix => translate("PausePendingSuffix");
        /// <summary>Gets the localized suffix applied when a job is completely stopped.</summary>
        public string StoppedSuffix => translate("StoppedSuffix");
        /// <summary>Gets the localized message displayed when a save completes successfully.</summary>
        public string SaveCompletedMessage => translate("SaveCompleted");
        /// <summary>Gets the localized message displayed when a save is stopped manually.</summary>
        public string SaveStoppedMessage => translate("SaveStopped");
        /// <summary>Gets the localized window title for the active save process.</summary>
        public string SaveWindowTitle => translate("SaveInProgressTitle");

        /// <summary>
        /// Gets or sets a dictionary containing the real-time execution states for each backup job, mapped by job name.
        /// </summary>
        public Dictionary<string, SaveJobState> JobStates { get; set; } = new Dictionary<string, SaveJobState>();

        /// <summary>
        /// Retrieves the <see cref="SaveJobState"/> for a specific backup job, creating it if it does not already exist.
        /// </summary>
        /// <param name="name">The name of the backup job.</param>
        /// <returns>The <see cref="SaveJobState"/> associated with the specified job.</returns>
        public SaveJobState GetJobState(string name)
        {
            if (!JobStates.ContainsKey(name))
            {
                JobStates[name] = new SaveJobState();
            }
            return JobStates[name];
        }

        /// <summary>Event triggered when a backup job fully transitions into a paused state.</summary>
        public event Action<string>? RealPaused;

        /// <summary>Event triggered when the background monitor detects the launch or closure of restricted business software.</summary>
        public Action<bool>? BusinessSoftwareStateChanged;

        /// <summary>Occurs when a ViewModel property value changes.</summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Triggers the <see cref="PropertyChanged"/> event.</summary>
        /// <param name="name">The name of the changed property.</param>
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Safely updates the UI thread to reflect that a backup job has actually paused, applying the correct UI suffixes.
        /// </summary>
        /// <param name="name">The name of the backup job.</param>
        public void TriggerRealPause(string name)
        {
            var state = GetJobState(name);
            state.IsPausePending = false;
            state.IsPaused = true;

            // Secure UI update using Dispatcher
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (state.CurrentFile != null)
                {
                    state.CurrentFile = state.CurrentFile
                        .Replace(PausePendingSuffix, "")
                        .Replace(PausedSuffix, "") + PausedSuffix;
                }
                RealPaused?.Invoke(name);
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModel"/> class.
        /// </summary>
        public ViewModel()
        {
            _model = new SaveModel();
            _languageService = LanguageService.getInstance();
        }

        // Methods

        /// <summary>
        /// Adds a new backup job configuration to the model.
        /// </summary>
        /// <param name="name">The job name.</param>
        /// <param name="sourcePath">The source directory path.</param>
        /// <param name="destinationPath">The destination directory path.</param>
        /// <param name="typeSave">The strategy type (complete or differential).</param>
        /// <param name="priorityExt">A list of priority file extensions.</param>
        /// <param name="completeSavePath">The reference path to a full backup (if applicable).</param>
        /// <returns><c>true</c> if successfully added; otherwise, <c>false</c>.</returns>
        public bool addSaveSpace(string name, string sourcePath, string destinationPath, string typeSave, List<string> priorityExt, string completeSavePath = "")
        {
            return _model.addSaveSpace(name, sourcePath, destinationPath, typeSave, priorityExt, completeSavePath);
        }

        /// <summary>
        /// Removes an existing backup job configuration from the model.
        /// </summary>
        /// <param name="name">The name of the job to remove.</param>
        /// <returns><c>true</c> if successfully removed; otherwise, <c>false</c>.</returns>
        public bool removeSaveSpace(string name)
        {
            return _model.removeSaveSpace(name);
        }

        /// <summary>
        /// Initiates the backup process asynchronously.
        /// </summary>
        /// <param name="name">The name of the job to execute.</param>
        /// <returns>A task representing the asynchronous operation, containing the success status.</returns>
        public async Task<bool> startSave(string name)
        {
            return await _model.startSave(name);
        }

        /// <summary>
        /// Initializes the UI state for a backup job, starts the background business software watcher, and begins the asynchronous execution.
        /// </summary>
        /// <param name="name">The name of the backup job to run.</param>
        /// <returns>A task containing the execution result.</returns>
        public async Task<bool> StartSaveAsync(string name)
        {
            // Retrieve the specific state object for this backup job
            var state = GetJobState(name);

            // Reset properties for a fresh run
            state.IsPausePending = false;
            state.IsPaused = false;
            state.CurrentFile = string.Empty;
            state.Progress = 0;

            using var cts = new CancellationTokenSource();
            _ = WatchBusinessSoftware(name, cts.Token);

            _model.SubscribeProgress(name, (p, f) =>
            {
                if (f == "Paused")
                {
                    state.IsPausePending = false;
                    state.IsPaused = true;

                    if (state.CurrentFile.EndsWith(PausePendingSuffix))
                    {
                        state.CurrentFile = state.CurrentFile.Replace(PausePendingSuffix, PausedSuffix);
                    }
                    else if (!state.CurrentFile.EndsWith(PausedSuffix))
                    {
                        state.CurrentFile += PausedSuffix;
                    }

                    return;
                }

                state.Progress = p;

                if (state.IsPausePending)
                {
                    if (!f.EndsWith(PausePendingSuffix))
                    {
                        state.CurrentFile = f + PausePendingSuffix;
                    }
                }
                else
                {
                    state.CurrentFile = f;
                }
            });

            try
            {
                return await _model.StartSaveAsync(name);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            finally
            {
                cts.Cancel();
            }
        }

        /// <summary>
        /// A background task that continuously polls the system to detect if restricted business software is launched or closed, pausing/resuming the backup accordingly.
        /// </summary>
        /// <param name="name">The name of the active backup job.</param>
        /// <param name="token">A cancellation token to terminate the watcher when the job ends.</param>
        private async Task WatchBusinessSoftware(string name, CancellationToken token)
        {
            bool wasRunning = false;
            while (!token.IsCancellationRequested)
            {
                bool isRunning = isBusinessSoftwareRunning(); // Assuming this is defined elsewhere
                if (isRunning && !wasRunning)
                {
                    wasRunning = true;
                    PauseSave(name); // Simulates a user pause
                    BusinessSoftwareStateChanged?.Invoke(true); // Tell View to turn yellow & lock button
                }
                else if (!isRunning && wasRunning)
                {
                    wasRunning = false;
                    ResumeSave(name); // Simulates a user resume
                    BusinessSoftwareStateChanged?.Invoke(false); // Tell View to turn green & unlock button
                }

                try
                {
                    // Check every 1.5 seconds
                    await Task.Delay(1500, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Requests a pause for the currently active backup job.
        /// </summary>
        /// <param name="name">The name of the job to pause.</param>
        public void PauseSave(string name)
        {
            var state = GetJobState(name);
            state.IsPausePending = true;
            state.IsPaused = false;

            _model.PauseSave(name);
        }

        /// <summary>
        /// Resumes a previously paused backup job.
        /// </summary>
        /// <param name="name">The name of the job to resume.</param>
        public void ResumeSave(string name)
        {
            var state = GetJobState(name);
            state.IsPausePending = false;
            state.IsPaused = false;

            _model.ResumeSave(name);
        }

        /// <summary>
        /// Stops and cancels an ongoing backup job.
        /// </summary>
        /// <param name="name">The name of the job to stop.</param>
        public void StopSave(string name)
        {
            _model.StopSave(name);
        }

        /// <summary>
        /// Retrieves all configured backup jobs from the model.
        /// </summary>
        /// <returns>A list of <see cref="SaveSpace"/> instances.</returns>
        public List<SaveSpace> getSaveSpaces()
        {
            return _model.getSaveSpaces();
        }

        /// <summary>
        /// Updates the application's current language via the LanguageService.
        /// </summary>
        /// <param name="languageCode">The target language code (e.g., "en", "fr").</param>
        public void setLanguage(string languageCode)
        {
            _languageService.setLanguage(languageCode);
        }

        /// <summary>
        /// Retrieves the currently active application language code.
        /// </summary>
        /// <returns>The active language code string.</returns>
        public string getLanguage()
        {
            return _languageService.getLanguage();
        }

        /// <summary>
        /// Sets the global formatting style for logs (e.g., JSON or XML).
        /// </summary>
        /// <param name="format">The target log format.</param>
        /// <returns><c>true</c> if successfully updated; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Retrieves the current global formatting style for logs.
        /// </summary>
        /// <returns>The current log format string.</returns>
        public string getLogsFormat()
        {
            return logger.getLogsFormat();
        }

        /// <summary>
        /// Fetches the translated string for a given translation key based on the active language.
        /// </summary>
        /// <param name="key">The dictionary key mapped to a translated string.</param>
        /// <returns>The translated text, or the key itself if no translation is found.</returns>
        public string translate(string key)
        {
            return _languageService.translate(key);
        }

        /// <summary>Gets the configured maximum file size threshold for concurrent transfers.</summary>
        /// <returns>The maximum size in kilobytes.</returns>
        public int getMaxSize()
        {
            return Config.Instance.getBiggestSize();
        }

        /// <summary>Sets the global maximum file size threshold for concurrent transfers.</summary>
        /// <param name="size">The size in kilobytes.</param>
        public void setMaxSize(int size)
        {
            Config.Instance.setBiggestSize(size);
        }

        /// <summary>Gets a value indicating whether logs should be sent to a remote server.</summary>
        public bool getBoolLogsOnServer()
        {
            return Config.Instance.getBoolLogsOnServer();
        }

        /// <summary>Gets a value indicating whether logs should be saved locally.</summary>
        public bool getBoolLogsOnLocal()
        {
            return Config.Instance.getBoolLogsOnLocal();
        }

        /// <summary>Sets whether logs should be transmitted to a remote server.</summary>
        public void setBoolLogsOnServer(bool value)
        {
            Config.Instance.setBoolLogsOnServer(value);
        }

        /// <summary>Sets whether logs should be persisted locally.</summary>
        public void setBoolLogsOnLocal(bool value)
        {
            Config.Instance.setBoolLogsOnLocal(value);
        }

        /// <summary>Gets the configured IP address of the remote logging server.</summary>
        public string getServerIp()
        {
            return Config.Instance.getServerIp();
        }

        /// <summary>Gets the configured port number of the remote logging server.</summary>
        public int getServerPort()
        {
            return Config.Instance.getServerPort();
        }

        /// <summary>Sets the IP address of the remote logging server.</summary>
        public void setServerIp(string ip)
        {
            Config.Instance.setServerIp(ip);
        }

        /// <summary>Sets the port number of the remote logging server.</summary>
        public void setServerPort(int port)
        {
            Config.Instance.setServerPort(port);
        }

        /// <summary>Attempts to re-establish a connection with the remote logging server.</summary>
        public void reconnectToServer()
        {
            logger.reconnectToServer();
        }

        /// <summary>Checks whether a connection to the remote logging server is currently active.</summary>
        public bool isConnectedToServer()
        {
            return logger.isConnectedToServer();
        }

        /// <summary>Checks whether any restricted business software is currently running on the system.</summary>
        public bool isBusinessSoftwareRunning()
        {
            return _model.isBusinessSoftwareRunning();
        }

        /// <summary>Sets the global encryption key used by the CryptoSoft tool.</summary>
        /// <param name="key">The new encryption key.</param>
        public void setEncryptionKey(string key)
        {
            Config.Instance.setEncryptionKey(key);
        }

        /// <summary>Gets the name of the restricted business software that pauses backups when launched.</summary>
        public string getBusinessSoftwareName()
        {
            return Config.Instance.getBusinessSoftwareName();
        }

        /// <summary>Sets the name of the restricted business software.</summary>
        /// <param name="name">The process name of the business software.</param>
        public void setBusinessSoftwareName(string name)
        {
            Config.Instance.setBusinessSoftwareName(name);
        }
    }
}