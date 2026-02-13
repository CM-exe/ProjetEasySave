using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Model;

public class BackupJobEventArgs(string jobName) : EventArgs {
    public string? JobName { get; } = jobName;
}
public class BackupJobProgressEventArgs(string jobName, int taskIndex) : BackupJobEventArgs(jobName) {
    public int CurrentTask { get; } = taskIndex;
}
public class BackupJobErrorEventArgs(string jobName, string errorMesssage) : BackupJobEventArgs(jobName) {
    public string? ErrorMessage { get; } = errorMesssage;
}
public class BackupJobCancelledEventArgs(string jobName, string cancelMessage) : BackupJobEventArgs(jobName) {
    public string? CancelMessage { get; } = cancelMessage;
}

public delegate void BackupJobEventHandler(object sender, BackupJobEventArgs e);
public delegate void BackupJobProgressEventHandler(object sender, BackupJobProgressEventArgs e);
public delegate void BackupJobErrorEventHandler(object sender, BackupJobErrorEventArgs e);
public delegate void BackupJobCancelledEventHandler(object sender, BackupJobCancelledEventArgs e);

public interface IBackupJob {
    /// <summary>
    /// Name of the backup job.
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// Source directory handler for the backup job.
    /// This is the directory from which files will be backed up.
    /// </summary>
    public IDirectoryHandler Source { get; }
    /// <summary>
    /// Destination directory handler for the backup job.
    /// This is the directory where the backed up files will be stored.
    /// </summary>
    public IDirectoryHandler Destination { get; }
    /// <summary>
    /// List of backup tasks to be executed.
    /// </summary>
    public List<IBackupTask> Tasks { get; }
    /// <summary>
    /// Current task index being executed.
    /// This is used to track the progress of the backup job.
    /// </summary>
    public int CurrentTaskIndex { get; }

    public IBackupTask CurrentTask { get; }

    public DateTime StartedAt { get; }

    public bool IsPaused { get; }


    /// <summary>
    /// Analyzes the source and destination directories to determine the files that need to be backed up.
    /// This method should be called before running the backup job.
    /// </summary>
    public abstract void Analyze();
    /// <summary>
    /// Runs the backup job.
    /// This method will execute the backup tasks in the order they are defined in the Tasks list.
    /// </summary>
    public Task Run();
    public void Stop();

    /// <summary>
    /// Pauses the backup job.
    /// </summary>
    public void Pause();

    /// <summary>
    /// Resumes the backup job.
    /// </summary>
    public void Resume();

    public event BackupJobEventHandler? BackupJobStarted;
    public event BackupJobProgressEventHandler? BackupJobProgress;
    public event BackupJobEventHandler? BackupJobPaused;
    public event BackupJobEventHandler? BackupJobResumed;
    public event BackupJobEventHandler? BackupJobFinished;
    public event BackupJobErrorEventHandler? BackupJobError;
    public event BackupJobCancelledEventHandler? BackupJobCancelled;
}

public abstract class BackupJob(string name, IDirectoryHandler source, IDirectoryHandler destination) : IBackupJob, INotifyPropertyChanged {
    private static double _ConcurrentTasksSize = 0;
    private static readonly object _SizeLock = new();
    private static readonly ManualResetEventSlim _CanProceed = new(true);
    public string Name { get; } = name;
    public IDirectoryHandler Source { get; } = source;
    public IDirectoryHandler Destination { get; } = destination;
    private bool _IsStopped = false;

    public List<IBackupTask> Tasks { get; } = [];
    public int CurrentTaskIndex { get; set; } = 0;
    public IBackupTask CurrentTask {
        get {
            if (CurrentTaskIndex < 0 || CurrentTaskIndex >= Tasks.Count) {
                return new BackupVoidTask();
            }
            return Tasks[CurrentTaskIndex];
        }
    }

    private Task? Task;

    public DateTime StartedAt {
        get;
        private set;
    }

    public bool IsPaused { get; private set; } = false;

    public event BackupJobEventHandler? BackupJobStarted;
    public event BackupJobProgressEventHandler? BackupJobProgress;
    public event BackupJobEventHandler? BackupJobPaused;
    public event BackupJobEventHandler? BackupJobResumed;
    public event BackupJobEventHandler? BackupJobFinished;
    public event BackupJobErrorEventHandler? BackupJobError;
    public event BackupJobCancelledEventHandler? BackupJobCancelled;

    public abstract void Analyze();

    public Task Run() {
        this.BackupJobStarted?.Invoke(this, new BackupJobEventArgs(this.Name));
        if (Tasks.Count == 0) {
            this.BackupJobFinished?.Invoke(this, new BackupJobEventArgs(this.Name));
            return Task.CompletedTask;
        }

        this._IsStopped = false;
        this.StartedAt = DateTime.Now;

        return this.Task = Task.Run(() => {
            ICrypto? crypto = null;

            try {
                crypto = Crypto.Acquire();
                for (this.CurrentTaskIndex = 0; this.CurrentTaskIndex < Tasks.Count - 1; this.CurrentTaskIndex++) {
                    if (this._IsStopped) {
                        this.BackupJobCancelled?.Invoke(this, new BackupJobCancelledEventArgs(this.Name, "Backup job was cancelled."));
                        return;
                    }

                    while (this.IsPaused) {
                        Thread.Sleep(100);
                    }

                    IBackupTask task = Tasks[this.CurrentTaskIndex];
                    task.StartTime = DateTime.Now;

                    // dev
                    //Thread.Sleep(500);


                    double taskSize = task.Source?.GetSize() ?? 0;

                    lock (_SizeLock) {
                        BackupJob._ConcurrentTasksSize += taskSize;

                        if (BackupJob._ConcurrentTasksSize > Configuration.Instance?.MaxConcurrentSize) {
                            _CanProceed.Wait();
                        }
                    }

                    try {
                        task.Run();
                        task.EndTime = DateTime.Now;
                        this.BackupJobProgress?.Invoke(this, new BackupJobProgressEventArgs(this.Name, this.CurrentTaskIndex));
                    } catch (Exception ex) {
                        this.BackupJobError?.Invoke(this, new BackupJobErrorEventArgs(this.Name, ex.Message));
                        return;
                    } finally {
                        lock (_SizeLock) {
                            BackupJob._ConcurrentTasksSize -= taskSize;
                            if (BackupJob._ConcurrentTasksSize < Configuration.Instance?.MaxConcurrentSize) {
                                _CanProceed.Set();
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                this.BackupJobError?.Invoke(this, new BackupJobErrorEventArgs(this.Name, ex.Message));
                return;
            } finally {
                crypto?.Release();
            }

            this.BackupJobFinished?.Invoke(this, new BackupJobEventArgs(this.Name));
        });
    }

    public void Stop() {
        this._IsStopped = true;
        this.BackupJobCancelled?.Invoke(this, new BackupJobCancelledEventArgs(this.Name, "Backup job was stopped."));
    }

    public void Pause() {
        this.IsPaused = true;
        this.OnPropertyChanged(nameof(this.IsPaused));
        this.BackupJobPaused?.Invoke(this, new BackupJobEventArgs(this.Name));
    }

    public void Resume() {
        this.IsPaused = false;
        this.OnPropertyChanged(nameof(this.IsPaused));
        this.BackupJobResumed?.Invoke(this, new BackupJobEventArgs(this.Name));
    }

    public void OnPropertyChanged(string propertyName) {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

