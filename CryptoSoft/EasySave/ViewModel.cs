using EasySave.Helpers;
using EasySave.Logger;
using EasySave.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave;

public class LanguageChangedEventArgs(string language) : EventArgs {
    public string? Language { get; set; } = language;
}
public class JobStateChangedEventArgs(IBackupJobState jobState) : EventArgs {
    public IBackupJobState? JobState { get; set; } = jobState;
}

public delegate void LanguageChangedEventHandler(object sender, LanguageChangedEventArgs e);
public delegate void JobStateChangedEventHandler(object sender, JobStateChangedEventArgs e);
public delegate void ConfigurationChangedEventHandler(object sender, ConfigurationChangedEventArgs e);

public interface IViewModel : INotifyPropertyChanged {

    /// <summary>
    /// List of all backup jobs.
    /// </summary>
    List<IBackupJob> BackupJobs { get; set; }

    /// <summary>
    /// Backup state
    /// </summary>
    IBackupState? BackupState { get; set; }

    /// <summary>
    /// Language used in the application.
    /// </summary>
    ILanguage Language { get; }

    /// <summary>
    /// Configuration object containing the application settings.
    /// </summary>
    IConfiguration Configuration { get; }

    ILogger Logger { get; }

    /// <summary>
    /// Called when the language is changed.
    /// </summary>
    void OnLanguageChanged(object sender, LanguageChangedEventArgs e);

    /// <summary>
    /// Called when the job state is changed.
    /// </summary>
    void OnJobStateChanged(object sender, JobStateChangedEventArgs e);

    /// <summary>
    /// Called when the configuration is changed.
    /// </summary>
    void OnConfigurationChanged(object sender, ConfigurationChangedEventArgs e);

    /// <summary>
    /// Called when a property is changed.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    void OnPropertyChanged(string propertyName);

    event LanguageChangedEventHandler? LanguageChanged;
    event JobStateChangedEventHandler? JobStateChanged;
    event ConfigurationChangedEventHandler? ConfigurationChanged;

    // Party configuration 🎉

    string BLanguage { get; set; }
    string StateFile { get; set; }
    string LogFile { get; set; }
    string CryptoFile { get; set; }
    string ExtensionsToEncrypt { get; set; }
    string EncryptionKey { get; set; }
    string Processes { get; set; }
    Commands Commands { get; }
}

public class ViewModel : IViewModel {
    public const string CONFIGURATION_PATH = "./configuration.json";
    private const int PAUSE_CHECK_DELAY_MS = 100;

    public List<IBackupJob> BackupJobs { get; set; } = [];
    public IBackupState? BackupState { get; set; }
    public ILanguage Language { get; set; }
    public IConfiguration Configuration { get; set; }
    public ILogger Logger { get; set; }
    public IProcessesDetector ProcessesDetector { get; set; }

    private ICrypto Crypto { get; set; }

    private SocketServer SocketServer { get; set; }

    public Commands Commands { get; } = new();

    public ViewModel() {
        ConfigurationManager configurationManager = new(typeof(ConfigurationJSONFile));
        this.Configuration = configurationManager.Load(ViewModel.CONFIGURATION_PATH);
        this.Configuration.ConfigurationChanged += this.OnConfigurationChanged;

        this.Language = Model.Language.Instance;
        this.Language.LanguageChanged += this.OnLanguageChanged;
        this.Language.Load();

        this.ProcessesDetector = new ProcessesDetector();
        this.ProcessesDetector.OneOrMoreProcessRunning += (sender, e) => {
            foreach (IBackupJob job in this.BackupJobs) {
                job.Pause();
                Logger?.Info(new Log {
                    JobName = job.Name,
                    Message = "One or more processes are running, stopping the backup job.",
                });
            }
        };

        this.ProcessesDetector.NoProcessRunning += (sender, e) => {
            foreach (IBackupJob job in this.BackupJobs) {
                job.Resume();
                Logger?.Info(new Log {
                    JobName = job.Name,
                    Message = "No processes are running, resuming the backup job.",
                });
            }
        };

        this.Logger = new Logger.Logger(this.Configuration.LogFile);

        this.Crypto = new Crypto(this.Configuration.CryptoFile, this.Configuration.CryptoKey);

        this.RegisterCommands();

        this.SocketServer = new SocketServer(this);
    }

    public void RegisterCommands() {
        this.Commands.RegisterCommand("run", (command) => this.RunCommandRun(command.Arguments), this.ParseJobList);
        this.Commands.RegisterCommand("add", (command) => {
            if (command.Arguments.Count < 4) {
                throw new Exception(this.Language.Translations["INVALID_INPUT"]);
            }
            this.RunCommandAdd(command.Arguments[0], command.Arguments[1], command.Arguments[2], command.Arguments[3]);
        });
        this.Commands.RegisterCommand("remove", (command) => {
            if (command.Arguments.Count < 1) {
                throw new Exception(this.Language.Translations["INVALID_INPUT"]);
            }
            this.RunCommandRemove(command.Arguments[0]);
        });
        this.Commands.RegisterCommand("language", (command) => {
            if (command.Arguments.Count < 1) {
                throw new Exception(this.Language.Translations["INVALID_INPUT"]);
            }
            this.RunCommandLanguage(command.Arguments[0]);
        });
        this.Commands.RegisterCommand("log", (command) => {
            if (command.Arguments.Count < 1) {
                throw new Exception(this.Language.Translations["INVALID_INPUT"]);
            }
            this.RunCommandLog(command.Arguments[0]);
        });
        this.Commands.RegisterCommand("pause", (command) => {
            if (command.Arguments.Count < 1) {
                throw new Exception(this.Language.Translations["INVALID_INPUT"]);
            }
            this.RunCommandPause(command.Arguments[0]);
        });
        this.Commands.RegisterCommand("resume", (command) => {
            if (command.Arguments.Count < 1) {
                throw new Exception(this.Language.Translations["INVALID_INPUT"]);
            }
            this.RunCommandResume(command.Arguments[0]);
        });
    }

    private List<string> ParseJobList(string jobList) {
        List<string> indexOrNameList = [];

        foreach (string indexOrName in jobList.Split(',')) {
            if (indexOrName.Contains('-')) {
                string[] indexes = indexOrName.Split('-');
                if (int.TryParse(indexes[0], out int first) && int.TryParse(indexes[1], out int last)) {
                    (first, last) = (Math.Min(first, last), Math.Max(first, last));
                    for (int i = first; i <= last; i++) {
                        if (!indexOrNameList.Contains(i.ToString())) {
                            indexOrNameList.Add(i.ToString());
                        }
                    }
                } else {
                    throw new Exception(this.Language.Translations["INVALID_INPUT"] + ": " + (string)indexOrName);
                }
            } else {
                if (!indexOrNameList.Contains(indexOrName)) {
                    indexOrNameList.Add(indexOrName);
                }
            }
        }

        return indexOrNameList;
    }

    private async void RunCommandRun(List<string> indexOrNameList) {
        List<IBackupJobConfiguration> jobsToRun = [];
        foreach (string indexOrName in indexOrNameList) {
            // Check if the indexOrName is a number
            if (int.TryParse(indexOrName, out int id)) {
                id = id - 1; // Adjust for 0-based index
                if (id < 0 || id >= this.Configuration.Jobs.Count) {
                    throw new Exception($"No backup job found with index: {indexOrName}");
                } else {
                    jobsToRun.Add(this.Configuration.Jobs[id]);
                }
            } else {
                IBackupJobConfiguration? job = this.Configuration.Jobs.FirstOrDefault(job => job.Name.Equals(indexOrName, StringComparison.OrdinalIgnoreCase));
                if (job is null) {
                    throw new Exception($"No backup job found with name: {indexOrName}");
                } else {
                    jobsToRun.Add(job);
                }
            }
        }

        // Check if there are any jobs to run
        if (jobsToRun.Count == 0) {
            throw new Exception("No backup jobs available.");
        }

        // Créer tous les BackupJobs à partir des configurations
        this.BackupJobs = BackupJobFactory.Create(jobsToRun);

        IStateFile file = new StateFile(this.Configuration.StateFile);
        using (this.BackupState = new BackupState(file)) {
            this.BackupState.JobStateChanged += this.OnJobStateChanged;

            // Séparer les jobs prioritaires et normaux APRÈS la création des BackupJobs
            var priorityJobs = GetPriorityJobs(this.BackupJobs, [.. Configuration.PriorityExtensions]);
            var normalJobs = GetNormalJobs(this.BackupJobs, [.. Configuration.PriorityExtensions]);

            // Debug: Log pour vérifier la séparation
            Logger?.Info(new Log {
                Message = $"Jobs prioritaires trouvés: {priorityJobs.Count}, Jobs normaux: {normalJobs.Count}"
            });

            // Debug: Vérifier l'état de chaque job prioritaire
            foreach (var job in priorityJobs) {
                Logger?.Info(new Log {
                    JobName = job.Name,
                    Message = $"Job prioritaire détecté: '{job.Name}', Source: '{job.Source?.GetPath() ?? "N/A"}'"
                });

                // Vérifier si le job peut être analysé
                try {
                    var entries = job.Source?.GetEntries();
                    Logger?.Info(new Log {
                        JobName = job.Name,
                        Message = $"Job '{job.Name}' contient {entries?.Count() ?? 0} entrées"
                    });
                } catch (Exception ex) {
                    Logger?.Error(new Log {
                        JobName = job.Name,
                        Message = $"Erreur lors de la vérification du job '{job.Name}': {ex.Message}"
                    });
                }
            }

            // Debug: Vérifier l'état du BackupState
            if (this.BackupState == null) {
                Logger?.Error(new Log {
                    Message = "ERREUR: BackupState est null!"
                });
            } else {
                Logger?.Info(new Log {
                    Message = "BackupState initialisé correctement"
                });
            }

            // Sémaphores pour contrôler la concurrence
            using SemaphoreSlim prioritySemaphore = new(Configuration.MaxConcurrentJobs);
            using SemaphoreSlim normalSemaphore = new(Configuration.MaxConcurrentJobs);

            try {
                // Exécuter d'abord les tâches prioritaires
                if (priorityJobs.Count > 0) {
                    Logger?.Info(new Log {
                        Message = "Démarrage des jobs prioritaires..."
                    });
                    await ExecutePriorityJobs(priorityJobs, prioritySemaphore);
                    Logger?.Info(new Log {
                        Message = "Jobs prioritaires terminés."
                    });
                }

                // Ensuite exécuter les tâches normales
                if (normalJobs.Count > 0) {
                    Logger?.Info(new Log {
                        Message = "Démarrage des jobs normaux..."
                    });
                    await ExecuteNormalJobs(normalJobs, normalSemaphore, () => false); // Plus de pause nécessaire
                    Logger?.Info(new Log {
                        Message = "Jobs normaux terminés."
                    });
                }
            } catch (Exception ex) {
                Logger?.Error(new Log {
                    Message = $"Erreur lors de l'exécution des jobs: {ex.Message}"
                });
                throw;
            }
        }
    }

    /// <summary>
    /// Vérifie l'état et la validité d'un job avant exécution
    /// </summary>
    private bool VerifyJobReadiness(IBackupJob job) {
        try {
            Logger?.Info(new Log {
                JobName = job.Name,
                Message = $"Vérification de la validité du job '{job.Name}'"
            });

            // Vérifier si la source existe et est accessible
            if (job.Source == null) {
                Logger?.Error(new Log {
                    JobName = job.Name,
                    Message = $"Source du job '{job.Name}' est null"
                });
                return false;
            }

            // Vérifier si la destination est définie
            if (job.Destination == null) {
                Logger?.Error(new Log {
                    JobName = job.Name,
                    Message = $"Destination du job '{job.Name}' est null"
                });
                return false;
            }

            // Essayer d'accéder aux entrées de la source
            var entries = job.Source.GetEntries();
            if (entries == null) {
                Logger?.Error(new Log {
                    JobName = job.Name,
                    Message = $"Impossible de récupérer les entrées du job '{job.Name}'"
                });
                return false;
            }

            int entryCount = entries.Count();
            Logger?.Info(new Log {
                JobName = job.Name,
                Message = $"Job '{job.Name}' validé: {entryCount} entrées trouvées"
            });

            return true;
        } catch (Exception ex) {
            Logger?.Error(new Log {
                JobName = job?.Name ?? "Unknown",
                Message = $"Erreur lors de la vérification du job: {ex.Message}"
            });
            return false;
        }
    }

    /// <summary>
    /// Exécute un seul job prioritaire
    /// </summary>
    private async Task ExecuteSinglePriorityJob(IBackupJob job, SemaphoreSlim semaphore) {
        Logger?.Info(new Log {
            JobName = job.Name,
            Message = $"Début d'exécution du job prioritaire '{job.Name}'"
        });

        // Vérifier la validité du job avant de prendre le sémaphore
        if (!VerifyJobReadiness(job)) {
            Logger?.Error(new Log {
                JobName = job.Name,
                Message = $"Job '{job.Name}' n'est pas prêt pour l'exécution"
            });
            return;
        }

        await semaphore.WaitAsync();
        Logger?.Info(new Log {
            JobName = job.Name,
            Message = $"Sémaphore acquis pour '{job.Name}'"
        });

        try {
            Logger?.Info(new Log {
                JobName = job.Name,
                Message = $"Analyse du job '{job.Name}'"
            });

            job.Analyze();

            Logger?.Info(new Log {
                JobName = job.Name,
                Message = $"Création de l'état du job '{job.Name}'"
            });

            this.BackupState?.CreateJobState(job);

            Logger?.Info(new Log {
                JobName = job.Name,
                Message = $"Lancement de l'exécution du job '{job.Name}'"
            });

            // Vérifier si des processus bloquants sont en cours avant de démarrer
            if (this.ProcessesDetector.HasOneOrMoreProcessRunning()) {
                Logger?.Info(new Log {
                    JobName = job.Name,
                    Message = $"Processus détectés, pause du job '{job.Name}'"
                });
                job.Pause();
            }

            Task jobTask = job.Run();
            Logger?.Info(new Log {
                JobName = job.Name,
                Message = $"Attente de la fin du job '{job.Name}'"
            });

            await jobTask;

            Logger?.Info(new Log {
                JobName = job.Name,
                Message = $"Job '{job.Name}' terminé avec succès"
            });
        } catch (Exception ex) {
            Logger?.Error(new Log {
                JobName = job.Name,
                Message = $"Erreur dans le job '{job.Name}': {ex.Message}\nStackTrace: {ex.StackTrace}"
            });
            throw;
        } finally {
            semaphore.Release();
            Logger?.Info(new Log {
                JobName = job.Name,
                Message = $"Sémaphore libéré pour '{job.Name}'"
            });
        }
    }

    /// <summary>
    /// Organise les jobs par priorité en fonction des extensions de fichiers
    /// </summary>
    private List<IBackupJob> OrganizeJobsByPriority(List<IBackupJob> jobs, List<string> priorityExtensions) {
        var priorityJobs = new List<IBackupJob>();
        var normalJobs = new List<IBackupJob>();

        foreach (var job in jobs) {
            if (JobHasPriorityFiles(job, priorityExtensions)) {
                priorityJobs.Add(job);
            } else {
                normalJobs.Add(job);
            }
        }

        // Retourner la liste avec les prioritaires en premier
        var organizedJobs = new List<IBackupJob>();
        organizedJobs.AddRange(priorityJobs);
        organizedJobs.AddRange(normalJobs);

        return organizedJobs;
    }

    /// <summary>
    /// Vérifie si un job contient des fichiers avec des extensions prioritaires
    /// </summary>
    private bool JobHasPriorityFiles(IBackupJob job, List<string> priorityExtensions) {
        try {
            // Récupérer tous les fichiers du job  
            var entries = job.Source.GetEntries();

            // Debug: Log des informations sur le job
            //Logger?.Info(new Log {
            //    JobName = job.Name,
            //    Message = $"Vérification des priorités pour le job '{job.Name}', {entries.Count()} entrées trouvées"
            //});

            // Vérifier si au moins un fichier a une extension prioritaire
            var fileHandlers = entries.OfType<IFileHandler>().ToList();

            foreach (var file in fileHandlers) {
                string extension = System.IO.Path.GetExtension(file.GetPath()).ToLowerInvariant()[1..];
                bool isPriority = priorityExtensions.Contains(extension);

                // Debug: Log pour chaque fichier vérifié
                //Logger?.Info(new Log {
                //    JobName = job.Name,
                //    Message = $"Fichier: {file.GetPath()}, Extension: {extension}, Prioritaire: {isPriority}"
                //});

                if (isPriority) {
                    //Logger?.Info(new Log {
                    //    JobName = job.Name,
                    //    Message = $"Job '{job.Name}' marqué comme prioritaire (extension {extension} trouvée)"
                    //});
                    return true;
                }
            }

            //Logger?.Info(new Log {
            //    JobName = job.Name,
            //    Message = $"Job '{job.Name}' marqué comme normal (aucune extension prioritaire trouvée)"
            //});

            return false;
        } catch (Exception ex) {
            // En cas d'erreur, considérer comme non prioritaire et logger l'erreur
            Logger?.Error(new Log {
                JobName = job?.Name ?? "Unknown",
                Message = $"Erreur lors de la vérification des priorités: {ex.Message}"
            });
            return false;
        }
    }

    /// <summary>
    /// Récupère les jobs prioritaires
    /// </summary>
    private List<IBackupJob> GetPriorityJobs(List<IBackupJob> jobs, List<string> priorityExtensions) {
        return jobs.Where(job => JobHasPriorityFiles(job, priorityExtensions)).ToList();
    }

    /// <summary>
    /// Récupère les jobs normaux
    /// </summary>
    private List<IBackupJob> GetNormalJobs(List<IBackupJob> jobs, List<string> priorityExtensions) {
        return jobs.Where(job => !JobHasPriorityFiles(job, priorityExtensions)).ToList();
    }

    /// <summary>
    /// Exécute les tâches prioritaires
    /// </summary>
    private async Task ExecutePriorityJobs(List<IBackupJob> priorityJobs, SemaphoreSlim semaphore) {
        Logger?.Info(new Log {
            Message = $"ExecutePriorityJobs: Début d'exécution de {priorityJobs.Count} jobs prioritaires"
        });

        var tasks = priorityJobs.Select(job => ExecuteSinglePriorityJob(job, semaphore)).ToArray();

        Logger?.Info(new Log {
            Message = $"ExecutePriorityJobs: {tasks.Length} tâches créées, attente de leur completion..."
        });

        try {
            await Task.WhenAll(tasks);
            Logger?.Info(new Log {
                Message = "ExecutePriorityJobs: Toutes les tâches prioritaires sont terminées"
            });
        } catch (Exception ex) {
            Logger?.Error(new Log {
                Message = $"ExecutePriorityJobs: Erreur lors de l'exécution: {ex.Message}"
            });
            throw;
        }
    }

    /// <summary>
    /// Exécute les tâches normales avec possibilité de pause
    /// </summary>
    private async Task ExecuteNormalJobs(List<IBackupJob> normalJobs, SemaphoreSlim semaphore, Func<bool> shouldPause) {
        await Task.WhenAll(normalJobs.Select(job => Task.Run(async () => {
            await semaphore.WaitAsync();
            try {
                // Vérifier si on doit mettre en pause avant de commencer
                while (shouldPause()) {
                    await Task.Delay(PAUSE_CHECK_DELAY_MS); // Attendre avant de revérifier
                }

                job.Analyze();
                this.BackupState?.CreateJobState(job);
                Task task = job.Run();

                // Vérification habituelle des processus
                if (this.ProcessesDetector.HasOneOrMoreProcessRunning()) job.Pause();

                await task;
            } catch (Exception ex) {
                Logger?.Error(new Log {
                    JobName = job.Name,
                    Message = $"Erreur dans le job '{job.Name}': {ex.Message}"
                });
            } finally {
                semaphore.Release();
            }
        })));
    }

    private void RunCommandAdd(string name, string source, string destination, string type) {
        if (this.Configuration.Jobs.FirstOrDefault(job => job.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) is not null) {
            throw new Exception($"A backup job with the name '{name}' already exists.");
        }

        if (this.Configuration.Jobs.FirstOrDefault(job =>
            job.Source.Equals(source, StringComparison.OrdinalIgnoreCase) &&
            job.Destination.Equals(destination, StringComparison.OrdinalIgnoreCase) &&
            job.Type.Equals(type, StringComparison.OrdinalIgnoreCase)
         ) is not null) {
            throw new Exception($"A backup job with the same source, destination and type already exists.");
        }

        // Create a new backup job configuration
        IBackupJobConfiguration newJob = new BackupJobConfiguration {
            Name = name,
            Source = source,
            Destination = destination,
            Type = type
        };

        // Add the new job to the configuration
        this.Configuration.AddJob(newJob);
    }

    private void RunCommandRemove(string indexOrName) {
        if (this.Configuration.Jobs.Count == 0) {
            throw new Exception("No backup jobs available.");
        }

        IBackupJobConfiguration? jobToRemove = null;
        // Check if the indexOrName is a number
        if (int.TryParse(indexOrName, out int id)) {
            id = id - 1; // Adjust for 0-based index
            if (id < 0 || id >= this.Configuration.Jobs.Count) {
                jobToRemove = this.Configuration.Jobs.FirstOrDefault(job => job.Name.Equals(indexOrName, StringComparison.OrdinalIgnoreCase));
            } else {
                // Remove the backup job by index
                jobToRemove = this.Configuration.Jobs[id];
            }
        } else {
            jobToRemove = this.Configuration.Jobs.FirstOrDefault(job => job.Name.Equals(indexOrName, StringComparison.OrdinalIgnoreCase));
        }

        if (jobToRemove is null) {
            throw new Exception($"No backup job found with name or index: {indexOrName}");
        }

        // Remove the backup job from the configuration
        this.Configuration.RemoveJob(jobToRemove);
    }

    private void RunCommandLanguage(string language) {
        this.Language.SetLanguage(language);
    }

    private void RunCommandLog(string logFilePath) {
        Configuration.LogFile = logFilePath;
        Logger.SetLogFile(logFilePath);
    }
    private void RunCommandPause(string nameOrId) {
        if (this.BackupState is null) {
            return;
        }
        for (int i = 0; i < this.BackupState.JobState.Count; i++) {
            IBackupJobState jobState = this.BackupState.JobState[i];
            if (jobState.BackupJob.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) || int.TryParse(nameOrId, out int id) && id - 1 == i
            ) {
                jobState.BackupJob.Pause();
            }
        }
    }
    private void RunCommandResume(string nameOrId) {
        if (this.BackupState is null) {
            return;
        }
        for (int i = 0; i < this.BackupState.JobState.Count; i++) {
            IBackupJobState jobState = this.BackupState.JobState[i];
            if (jobState.BackupJob.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase) || int.TryParse(nameOrId, out int id) && id - 1 == i) {
                jobState.BackupJob.Resume();
            }
        }
    }

    public void OnLanguageChanged(object sender, LanguageChangedEventArgs e) {
        this.LanguageChanged?.Invoke(this, e);
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Language)));
    }

    public void OnJobStateChanged(object sender, JobStateChangedEventArgs e) {
        this.JobStateChanged?.Invoke(this, e);

        switch (e.JobState?.State) {
            case State.IN_PROGRESS:
                IBackupTask task = e.JobState.BackupJob.Tasks[e.JobState.BackupJob.CurrentTaskIndex];
                this.Logger.Info(new Log {
                    JobName = e.JobState.BackupJob.Name,
                    Filesize = task.Source?.GetSize() ?? 0,
                    Source = task.Source?.GetPath() ?? string.Empty,
                    Destination = task.Destination?.GetPath() ?? string.Empty,
                    TaskType = task is BackupCopyTask ? "Copy" : "Remove",
                    TransfertDuration = task.GetDuration()
                });
                break;
            case State.CANCEL:
                this.Logger.Info(new Log {
                    JobName = e.JobState.BackupJob.Name,
                    Message = "Backup job was cancelled."
                });
                break;
            case State.ERROR:
                this.Logger.Error(new Log {
                    JobName = e.JobState.BackupJob.Name,
                    Message = "An error occurred during the backup job."
                });
                break;

        }
    }

    // Party configuration 🎉
    public string BLanguage {
        get => Configuration.Language;
        set {
            Language.SetLanguage(value);
            OnPropertyChanged(nameof(BLanguage));
            OnPropertyChanged(nameof(Language));
        }
    }

    public string StateFile {
        get => Configuration.StateFile;
        set {
            Configuration.StateFile = value;
            OnPropertyChanged(nameof(StateFile));
        }
    }

    public string LogFile {
        get => Configuration.LogFile;
        set {
            Configuration.LogFile = value;
            OnPropertyChanged(nameof(LogFile));
        }
    }

    public string CryptoFile {
        get => Configuration.CryptoFile;
        set {
            Configuration.CryptoFile = value;
            OnPropertyChanged(nameof(CryptoFile));
        }
    }

    public string ExtensionsToEncrypt {
        get => string.Join(";", Configuration.CryptoExtensions);
        set {
            Configuration.CryptoExtensions = [.. value.Split(';')];
            OnPropertyChanged(nameof(ExtensionsToEncrypt));
        }
    }

    public string EncryptionKey {
        get => Configuration.CryptoKey;
        set {
            Configuration.CryptoKey = value;
            OnPropertyChanged(nameof(EncryptionKey));
        }
    }

    public string Processes {
        get => string.Join(";", Configuration.Processes);
        set {
            Configuration.Processes = [.. value.Split(";")];
            OnPropertyChanged(nameof(Processes));
        }
    }

    public void OnConfigurationChanged(object sender, ConfigurationChangedEventArgs e) {
        this.ConfigurationChanged?.Invoke(this, e);

        if (e.PropertyName == nameof(IConfiguration.StateFile) && this.BackupState is not null) {
            this.BackupState.File = new StateFile(this.Configuration.StateFile);
        }
    }

    public void OnPropertyChanged(string propertyName) {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event LanguageChangedEventHandler? LanguageChanged;
    public event JobStateChangedEventHandler? JobStateChanged;
    public event ConfigurationChangedEventHandler? ConfigurationChanged;
}