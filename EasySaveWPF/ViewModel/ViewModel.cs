using ProjetEasySave.Model;
using ProjetEasySave.Utils;
using System.Diagnostics;
using EasyLog;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjetEasySave.ViewModel
{
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Constructor
        public ViewModel()
        {
            _model = new SaveModel();
            _languageService = new LanguageService();
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
            _model.SubscribeProgress(name, (p, f) =>
            {
                if (f == "Paused")
                {
                    IsPausePending = false;

                    if (CurrentFile.EndsWith(PausePendingSuffix))
                    {
                        CurrentFile = CurrentFile.Replace(PausePendingSuffix, PausedSuffix);
                    }
                    else if (!CurrentFile.EndsWith(PausedSuffix))
                    {
                        CurrentFile += PausedSuffix;
                    }

                    return;
                }

                Progress = p;

                if (IsPausePending)
                {
                    if (!f.EndsWith(PausePendingSuffix))
                    {
                        CurrentFile = f + PausePendingSuffix;
                    }
                }
                else
                {
                    CurrentFile = f;
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
        }

        public void PauseSave(string name)
        {
            IsPausePending = true;
            _model.PauseSave(name);
        }

        public void ResumeSave(string name)
        {
            IsPausePending = false;
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
    }
}
