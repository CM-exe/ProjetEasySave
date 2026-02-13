using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EasySave.Model;

/// <summary>
/// Event arguments for the ConfigurationChanged event
/// </summary>
public class ConfigurationChangedEventArgs : EventArgs {
    // Property name that changed
    public string? PropertyName { get; set; }
}
// delegate for the ConfigurationChanged event
public delegate void ConfigurationChangedEventHandler(object sender, ConfigurationChangedEventArgs e);

/// <summary>
/// Configuration interface
/// </summary>
public interface IConfiguration {
    public const string DEFAULT_LANGUAGE = "FR";
    public const string DEFAULT_STATE_FILE = "state.json";
    public const string DEFAULT_LOG_FILE = "logs.json";
    public const string DEFAULT_CRYPTO_FILE = "CryptoSoft/CryptoSoft.exe";
    public const string DEFAULT_CRYPTO_KEY = "7A2F8D15E9C3B6410D5F78A92E64B0C3DB91A527F836E45C0B2D7498C1E5A3F6";
    public const int DEFAULT_MAX_CONCURRENT_JOBS = 5;
    public const int DEFAULT_MAX_CONCURRENT_SIZE = 100 * 1024 * 1024; // 100 MB

    string Language { get; set; }
    string StateFile { get; set; }
    string LogFile { get; set; }
    ObservableCollection<string> Processes { get; set; }
    ObservableCollection<string> CryptoExtensions { get; set; }
    string CryptoFile { get; set; }
    string CryptoKey { get; set; }
    ObservableCollection<IBackupJobConfiguration> Jobs { get; set; }
    ObservableCollection<string> PriorityExtensions { get; set; }
    int MaxConcurrentJobs { get; set; }
    int MaxConcurrentSize { get; set; }

    public void AddJob(IBackupJobConfiguration jobConfiguration);
    public void RemoveJob(IBackupJobConfiguration jobConfiguration);
    public JsonObject ToJSON();

    event ConfigurationChangedEventHandler ConfigurationChanged;
}

public class Configuration : IConfiguration {
    /// <summary>
    /// Singleton instance of Configuration
    public static Configuration? Instance { get; private set; }

    // private fields
    // Language of the application
    private static string? _Language;
    // State file of the application
    private static string? _StateFile;
    // Log file of the application
    private static string? _LogFile;
    // List of processes to detect
    private static string? _CryptoKey;
    private static ObservableCollection<string>? _Processes;
    // List of crypt extensions
    private static ObservableCollection<string>? _CryptExtensions;
    // Crypto file of the application
    private static string? _CryptoFile;
    // List of jobs
    private static ObservableCollection<IBackupJobConfiguration>? _Jobs;
    private static ObservableCollection<string>? _PriorityExtensions;
    private static int _MaxConcurrentJobs = IConfiguration.DEFAULT_MAX_CONCURRENT_JOBS;
    private static int _MaxConcurrentSize = IConfiguration.DEFAULT_MAX_CONCURRENT_SIZE;

    /// <summary>
    /// Language of the application
    /// </summary>
    public string Language {
        // getting the language default value
        get => _Language ?? IConfiguration.DEFAULT_LANGUAGE;
        // setting the language and raising the event
        set {
            // load the language
            _Language = value;
            // set the configuration changed event
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                // set the new language to save
                PropertyName = nameof(Language)
            });
        }
    }
    /// <summary>
    /// state file of the application
    /// </summary>
    public string StateFile {
        // check if the state file is set
        // if not, throw an exception
        get => _StateFile ?? throw new InvalidOperationException("State file is not set");
        // set the state file and raise the event
        set {
            // set the state file
            _StateFile = value;
            // set the configuration changed event
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                // set the new state file to save
                PropertyName = nameof(StateFile)
            });
        }
    }


    public string CryptoKey {
        get => _CryptoKey ?? IConfiguration.DEFAULT_CRYPTO_KEY;
        set {
            // set the state file
            _CryptoKey = value;
            // set the configuration changed event
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                // set the new state file to save
                PropertyName = nameof(CryptoKey)
            });
        }
    }


    public string LogFile {
        // check if the log file is set
        // if not, throw an exception
        get => _LogFile ?? throw new InvalidOperationException("Log file is not set");
        // set the log file and raise the event
        set {
            // set the log file
            _LogFile = value;
            // set the configuration changed event
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                // set the new log file to save
                PropertyName = nameof(LogFile)
            });
        }
    }

    public ObservableCollection<string> Processes {
        // getting the processes list
        get => _Processes ?? new ObservableCollection<string>();
        // setting the processes list and raising the event
        set {
            _Processes = value;
            // set the configuration changed event
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                // set the new processes list to save
                PropertyName = nameof(Processes)
            });
        }
    }

    public ObservableCollection<string> CryptoExtensions {
        // getting the crypt extensions list
        get => _CryptExtensions ?? new ObservableCollection<string>();
        // setting the crypt extensions list and raising the event
        set {
            _CryptExtensions = value;
            // set the configuration changed event
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                // set the new crypt extensions list to save
                PropertyName = nameof(CryptoExtensions)
            });
        }
    }


    /// <summary>
    /// List of jobs
    /// </summary>
    public ObservableCollection<IBackupJobConfiguration> Jobs {
        // getting the jobs list
        get => _Jobs ?? [];
        // setting the jobs list and raising the event
        set {
            _Jobs = value;
            // set the configuration changed event
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                // set the new jobs list to save
                PropertyName = nameof(Jobs)
            });
        }
    }

    public ObservableCollection<string> PriorityExtensions {
        // getting the priority extensions list
        get => _PriorityExtensions ?? new ObservableCollection<string>();
        // setting the priority extensions list and raising the event
        set {
            _PriorityExtensions = value;
            // set the configuration changed event
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                // set the new priority extensions list to save
                PropertyName = nameof(PriorityExtensions)
            });
        }
    }

    public int MaxConcurrentJobs {
        // getting the max concurrent jobs value
        get => _MaxConcurrentJobs;
        // setting the max concurrent jobs value and raising the event
        set {
            _MaxConcurrentJobs = value;
            // set the configuration changed event
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                // set the new max concurrent jobs value to save
                PropertyName = nameof(MaxConcurrentJobs)
            });
        }
    }

    public int MaxConcurrentSize {
        // getting the max concurrent size value
        get => _MaxConcurrentSize;
        // setting the max concurrent size value and raising the event
        set {
            _MaxConcurrentSize = value;
            // set the configuration changed event
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                // set the new max concurrent size value to save
                PropertyName = nameof(MaxConcurrentSize)
            });
        }
    }

    public string CryptoFile {
        // check if the crypto file is set
        // if not, throw an exception
        get => _CryptoFile ?? throw new InvalidOperationException("Crypto file is not set");
        // set the crypto file and raise the event
        set {
            // set the crypto file
            _CryptoFile = value;
            // set the configuration changed event
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                // set the new crypto file to save
                PropertyName = nameof(CryptoFile)
            });
        }
    }

    public Configuration(ConfigurationDTO configuration) {
        // check if the instance is already initialized
        if (Configuration.Instance != null) {
            // if the instance is already created, throw an exception
            throw new InvalidOperationException("Configuration is already initialized.");
        }
        // assign the values configuration
        this.Language = configuration.Language ?? IConfiguration.DEFAULT_LANGUAGE;
        this.StateFile = configuration.StateFile ?? IConfiguration.DEFAULT_STATE_FILE;
        this.LogFile = configuration.LogFile ?? IConfiguration.DEFAULT_LOG_FILE;
        this.CryptoFile = configuration.CryptoFile ?? IConfiguration.DEFAULT_CRYPTO_FILE;
        this.CryptoKey = configuration.CryptoKey ?? IConfiguration.DEFAULT_CRYPTO_KEY;
        this.Processes = new ObservableCollection<string>(configuration.Processes ?? []);
        this.CryptoExtensions = new ObservableCollection<string>(configuration.CryptoExtensions ?? []);
        this.Jobs = new ObservableCollection<IBackupJobConfiguration>(configuration.Jobs?.Select(j => new BackupJobConfiguration {
            Name = j.Name,
            Source = j.Source,
            Destination = j.Destination,
            Type = j.Type
        }) ?? []);
        this.PriorityExtensions = new ObservableCollection<string>(configuration.PriorityExtensions ?? []);
        this.MaxConcurrentJobs = configuration.MaxConcurrentJobs > 0 ? configuration.MaxConcurrentJobs : IConfiguration.DEFAULT_MAX_CONCURRENT_JOBS;
        this.MaxConcurrentSize = configuration.MaxConcurrentSize > 0 ? configuration.MaxConcurrentSize : IConfiguration.DEFAULT_MAX_CONCURRENT_SIZE;
        // Subscribe to the CollectionChanged event for Processes and CryptoExtensions
        this.Processes.CollectionChanged += Processes_CollectionChanged;
        this.CryptoExtensions.CollectionChanged += CryptoExtensions_CollectionChanged;
        this.PriorityExtensions.CollectionChanged += PriorityExtensions_CollectionChanged;
        // Subscribe to the JobConfigurationChanged event for each job
        foreach (IBackupJobConfiguration jobConfiguration in this.Jobs) {
            jobConfiguration.JobConfigurationChanged += (sender, args) => {
                // Raise the ConfigurationChanged event when a job configuration changes
                this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                    // Set the property name to Jobs
                    PropertyName = nameof(Jobs)
                });
            };
        }
        Configuration.Instance = this;
    }

    private void CryptoExtensions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
        this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
            PropertyName = nameof(CryptoExtensions)
        });
    }

    private void Processes_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
        this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
            PropertyName = nameof(Processes)
        });
    }

    private void PriorityExtensions_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
        this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
            PropertyName = nameof(PriorityExtensions)
        });
    }

    /// <summary>
    /// add a new job to the configuration
    /// </summary>
    /// param name="jobConfiguration"></param>
    public void AddJob(IBackupJobConfiguration jobConfiguration) {
        // add the job to the list of jobs
        this.Jobs.Add(jobConfiguration);

        // subscribe to the JobConfigurationChanged event
        jobConfiguration.JobConfigurationChanged += (sender, args) => {
            // raise the ConfigurationChanged event when a job configuration changes
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
                // set the property name to Jobs
                PropertyName = nameof(Jobs)
            });
        };

        // raise the ConfigurationChanged event
        this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
            // set the property Jobs to save
            PropertyName = nameof(Jobs)
        });
    }

    /// <summary>
    /// remove a job from the configuration
    /// </summary>
    /// param name="jobConfiguration"></param>
    public void RemoveJob(IBackupJobConfiguration jobConfiguration) {
        // remove the job from the list of jobs
        this.Jobs.Remove(jobConfiguration);
        // send the event
        // raise the ConfigurationChanged event
        this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs {
            // set the property Jobs to save
            PropertyName = nameof(Jobs)
        });
    }

    public event ConfigurationChangedEventHandler? ConfigurationChanged;

    public JsonObject ToJSON() {

        return new JsonObject {
            ["Language"] = this.Language,
            ["StateFile"] = this.StateFile,
            ["LogFile"] = this.LogFile,
            ["CryptoFile"] = this.CryptoFile,
            ["CryptoKey"] = this.CryptoKey,
            ["Processes"] = new JsonArray([.. this.Processes]),
            ["CryptoExtensions"] = new JsonArray([.. this.CryptoExtensions]),
            ["Jobs"] = new JsonArray([.. this.Jobs.Select(j => j.ToJSON())]),
            ["PriorityExtensions"] = new JsonArray([.. this.PriorityExtensions]),
            ["MaxConcurrentJobs"] = this.MaxConcurrentJobs,
            ["MaxConcurrentSize"] = this.MaxConcurrentSize
        };
    }

    public override string ToString() {
        return JsonSerializer.Serialize(this.ToJSON());
    }
}

