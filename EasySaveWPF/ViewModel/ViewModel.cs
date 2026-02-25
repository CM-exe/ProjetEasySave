using ProjetEasySave.Model;
using ProjetEasySave.Utils;
using System.Diagnostics;
using EasyLog;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjetEasySave.ViewModel
{
    public class SaveJobState : INotifyPropertyChanged
    {
        private int _progress;
        public int Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }

        private string _currentFile = string.Empty;
        public string CurrentFile
        {
            get => _currentFile;
            set { _currentFile = value; OnPropertyChanged(); }
        }

        private bool _isPausePending = false;
        public bool IsPausePending
        {
            get => _isPausePending;
            set { _isPausePending = value; OnPropertyChanged(); }
        }

        // Added to differentiate between "Waiting to pause" and "Actually paused"
        private bool _isPaused = false;
        public bool IsPaused
        {
            get => _isPaused;
            set { _isPaused = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ViewModel : INotifyPropertyChanged
    {
        // Attributes
        private SaveModel _model;
        private LanguageService _languageService;
        private readonly Logger logger = Logger.getInstance(Config.Instance); // Load logger

        // Translations
        public string CurrentFileLabel => translate("CurrentFileLabel");
        public string PausedSuffix => translate("PausedSuffix");
        public string PausePendingSuffix => translate("PausePendingSuffix");
        public string StoppedSuffix => translate("StoppedSuffix");
        public string SaveCompletedMessage => translate("SaveCompleted");
        public string SaveStoppedMessage => translate("SaveStopped");
        public string SaveWindowTitle => translate("SaveInProgressTitle");

        // Dictionary to store the specific state of each save job
        public Dictionary<string, SaveJobState> JobStates { get; set; } = new Dictionary<string, SaveJobState>();

        // Helper method to retrieve or create a job state safely
        public SaveJobState GetJobState(string name)
        {
            if (!JobStates.ContainsKey(name))
            {
                JobStates[name] = new SaveJobState();
            }
            return JobStates[name];
        }

        public event Action<string>? RealPaused;

        public Action<bool>? BusinessSoftwareStateChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Method to trigger the real pause transition safely
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

        // Constructor
        public ViewModel()
        {
            _model = new SaveModel();
            _languageService = LanguageService.getInstance();
        }

        // Methods
        public bool addSaveSpace(string name, string sourcePath, string destinationPath, string typeSave, List<string> priorityExt, string completeSavePath = "")
        {
            return _model.addSaveSpace(name, sourcePath, destinationPath, typeSave, priorityExt, completeSavePath);
        }

        public bool removeSaveSpace(string name)
        {
            return _model.removeSaveSpace(name);
        }

        public async Task<bool> startSave(string name)
        {
            return await _model.startSave(name);
        }

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

        public void PauseSave(string name)
        {
            var state = GetJobState(name);
            state.IsPausePending = true;
            state.IsPaused = false;

            _model.PauseSave(name);
        }

        public void ResumeSave(string name)
        {
            var state = GetJobState(name);
            state.IsPausePending = false;
            state.IsPaused = false;

            _model.ResumeSave(name);
        }

        public void StopSave(string name)
        {
            _model.StopSave(name);
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

        public int getMaxSize()
        {
            return Config.Instance.getBiggestSize();
        }

        public void setMaxSize(int size)
        {
            Config.Instance.setBiggestSize(size);
        }

        public bool getBoolLogsOnServer()
        {
            return Config.Instance.getBoolLogsOnServer();
        }

        public bool getBoolLogsOnLocal()
        {
            return Config.Instance.getBoolLogsOnLocal();
        }

        public void setBoolLogsOnServer(bool value)
        {
            Config.Instance.setBoolLogsOnServer(value);
        }

        public void setBoolLogsOnLocal(bool value)
        {
            Config.Instance.setBoolLogsOnLocal(value);
        }

        public string getServerIp()
        {
            return Config.Instance.getServerIp();
        }

        public int getServerPort()
        {
            return Config.Instance.getServerPort();
        }

        public void setServerIp(string ip)
        {
            Config.Instance.setServerIp(ip);
        }

        public void setServerPort(int port)
        {
            Config.Instance.setServerPort(port);
        }

        public void reconnectToServer()
        {
            logger.reconnectToServer();
        }

        public bool isConnectedToServer()
        {
            return logger.isConnectedToServer();
        }

        public bool isBusinessSoftwareRunning()
        {
            return _model.isBusinessSoftwareRunning();
        }
        public void setEncryptionKey(string key)
        {
            Config.Instance.setEncryptionKey(key);
        }
    }
}
