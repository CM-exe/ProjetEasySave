using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace EasySave.Model;


/// <summary>
/// Represents the various states a backup job can be in.
/// </summary>
public enum State {
    /// <summary>
    /// The job is created and ready to run.
    /// </summary>
    NOT_STARTED,
    /// <summary>
    /// The job is running.
    /// </summary>
    ACTIVE,
    /// <summary>
    /// The job has completed successfully.
    /// </summary>
    END,
    /// <summary>
    /// An error occurred during the job execution.
    /// </summary>
    ERROR,
    /// <summary>
    /// The job is currently running.
    /// </summary>
    IN_PROGRESS,
    /// <summary>
    /// The job is paused.
    /// </summary>
    PAUSED,
    /// <summary>
    /// The job has resumed after a pause.
    /// </summary>
    RESUMED,
    /// <summary>
    /// The job has been cancelled.
    /// </summary>
    CANCEL,
}
/// <summary>
/// Represents the state of a backup job at a given time.
/// Tracks progress, size, and lifecycle events.
/// </summary>
public interface IBackupJobState : IDisposable {
    /// <summary>
    /// The backup job associated with this state instance.
    /// </summary>
    public IBackupJob BackupJob { get; set; }
    /// <summary>
    /// The path of the source directory being backed up.
    /// </summary>
    public string SourceFilePath { get; set; }
    /// <summary>
    /// The path of the destination directory where files are backed up.
    /// </summary>
    public string DestinationFilePath { get; set; }
    /// <summary>
    /// Current state of the backup job.
    /// </summary>
    public State State { get; set; }
    /// <summary>
    /// Total number of files to copy in this job.
    /// </summary>
    public double TotalFilesToCopy { get; set; }
    /// <summary>
    /// Total size of files to copy (in bytes).
    /// </summary>
    public double TotalFilesSize { get; set; }
    /// <summary>
    /// Number of files remaining to copy.
    /// </summary>
    public double FilesLeft { get; set; }
    /// <summary>
    /// Size of files left to copy (in bytes).
    /// </summary>
    public double FilesLeftSize { get; set; }
    /// <summary>
    /// Current progression of the job, from 0 to 100.
    /// </summary>
    public int Progression { get; set; }

    /// <summary>
    /// Called when the job starts.
    /// Used to initialize state.
    /// </summary>
    public void OnJobStarted(object sender, BackupJobEventArgs e);
    /// <summary>
    /// Called when progress is made in the backup job.
    /// Used to update progress and remaining files.
    /// </summary>
    public void OnJobProgress(object sender, BackupJobProgressEventArgs e);
    /// <summary>
    /// Called when the job is paused.
    /// </summary>
    public void OnJobPaused(object sender, BackupJobEventArgs e);
    /// <summary>
    /// Called when the job resumes from a pause.
    /// </summary>
    public void OnJobResumed(object sender, BackupJobEventArgs e);
    /// <summary>
    /// Called when the job finishes successfully.
    /// </summary>
    public void OnJobFinished(object sender, BackupJobEventArgs e);
    /// <summary>
    /// Called when the job is cancelled by the user or system.
    /// </summary>
    public void OnJobCancelled(object sender, BackupJobEventArgs e);

    /// <summary>
    /// Event triggered whenever the state of the job changes.
    /// This can be used for saving state or updating the UI.
    /// </summary>
    public event JobStateChangedEventHandler JobStateChanged;
}

public class BackupJobState : IBackupJobState {
    public IBackupJob BackupJob { get; set; }
    public string SourceFilePath { get; set; }
    public string DestinationFilePath { get; set; }
    public State State { get; set; } = State.NOT_STARTED;
    public double TotalFilesToCopy { get; set; }
    public double TotalFilesSize { get; set; }
    public double FilesLeft { get; set; }
    public double FilesLeftSize { get; set; }
    public int Progression { get; set; }

    public BackupJobState(IBackupJob backupJob) {
        this.BackupJob = backupJob;
        this.SourceFilePath = backupJob.Source.GetPath();
        this.DestinationFilePath = backupJob.Destination.GetPath();
        List<BackupCopyTask> tasks = [.. backupJob.Tasks.OfType<BackupCopyTask>()];
        this.TotalFilesToCopy = tasks.Count;
        this.TotalFilesSize = tasks.Sum(t => t.Source?.GetSize() ?? 0);
        this.FilesLeft = TotalFilesToCopy;
        this.FilesLeftSize = TotalFilesSize;
        this.Progression = 0;

        backupJob.BackupJobStarted += OnJobStarted;
        backupJob.BackupJobProgress += OnJobProgress;
        backupJob.BackupJobPaused += OnJobPaused;
        backupJob.BackupJobResumed += OnJobResumed;
        backupJob.BackupJobFinished += OnJobFinished;
        backupJob.BackupJobCancelled += OnJobCancelled;
        backupJob.BackupJobError += OnJobError;
    }

    /// <summary>
    /// Raises the <c>JobStateChanged</c> event to notify subscribers of a state change in the backup job.
    /// This is typically called after a change in job progress, pause, resume, finish, or cancellation.
    /// </summary>
    private void RaiseStateChanged() {
        this.JobStateChanged?.Invoke(this, new JobStateChangedEventArgs(this));
    }

    public void OnJobStarted(object sender, BackupJobEventArgs e) {
        this.State = State.ACTIVE;
        this.RaiseStateChanged();
    }
    public void OnJobProgress(object sender, BackupJobProgressEventArgs e) {
        this.State = State.IN_PROGRESS;

        IBackupTask task = this.BackupJob.Tasks[e.CurrentTask];
        if (task is BackupCopyTask copyTask) {
            this.FilesLeft--;
            this.FilesLeftSize -= copyTask.Source?.GetSize() ?? 0;
        }

        this.SourceFilePath = task.Source?.GetPath() ?? string.Empty;
        this.DestinationFilePath = task.Destination?.GetPath() ?? string.Empty;
        this.Progression = this.BackupJob.Tasks.Count > 0 ? (int)Math.Round((double)this.BackupJob.CurrentTaskIndex / this.BackupJob.Tasks.Count * 100) : 0;

        this.RaiseStateChanged();
    }
    public void OnJobPaused(object sender, BackupJobEventArgs e) {
        this.State = State.PAUSED;
        this.RaiseStateChanged();
    }
    public void OnJobResumed(object sender, BackupJobEventArgs e) {
        this.State = State.RESUMED;
        this.RaiseStateChanged();
    }
    public void OnJobFinished(object sender, BackupJobEventArgs e) {
        this.State = State.END;
        this.RaiseStateChanged();
    }
    public void OnJobCancelled(object sender, BackupJobEventArgs e) {
        this.State = State.CANCEL;
        this.RaiseStateChanged();
    }
    public void OnJobError(object sender, BackupJobEventArgs e) {
        this.State = State.ERROR;
        this.RaiseStateChanged();
    }

    public void Dispose() {
        this.BackupJob.BackupJobStarted -= this.OnJobStarted;
        this.BackupJob.BackupJobProgress -= this.OnJobProgress;
        this.BackupJob.BackupJobPaused -= this.OnJobPaused;
        this.BackupJob.BackupJobResumed -= this.OnJobResumed;
        this.BackupJob.BackupJobFinished -= this.OnJobFinished;
        this.BackupJob.BackupJobCancelled -= this.OnJobCancelled;
        this.BackupJob.BackupJobError -= this.OnJobError;
        this.BackupJob = null!;

        GC.SuppressFinalize(this);
    }

    ~BackupJobState() {
        Dispose();
    }

    public event JobStateChangedEventHandler? JobStateChanged;

}

