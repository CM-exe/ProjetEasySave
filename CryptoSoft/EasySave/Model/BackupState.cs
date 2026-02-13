using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Model;

public interface IBackupState : IDisposable {
    /// <summary>
    /// Gets or sets the file responsible for saving the state.
    /// </summary>
    public IStateFile File { get; set; }
    /// <summary>
    /// Gets or sets the list of job states being tracked.
    /// </summary>
    public List<IBackupJobState> JobState { get; set; }
    /// <summary>
    /// Creates and adds a new backup job state from a given job,
    /// and subscribes to its JobStateChanged event for tracking changes.
    /// </summary>
    public IBackupJobState CreateJobState(IBackupJob backupJob);
    /// <summary>
    /// Called when any job state changes to persist the updated state.
    /// </summary>
    public void OnJobStateChanged(object sender, JobStateChangedEventArgs e);

    public string ToJSON(bool indent = true);

    public event JobStateChangedEventHandler JobStateChanged;
}

/// <summary>
/// Singleton class that manages backup job states and persists them.
/// </summary>
public class BackupState(IStateFile file) : IBackupState {
    public IStateFile File { get; set; } = file;
    public List<IBackupJobState> JobState { get; set; } = [];

    public void OnJobStateChanged(object sender, JobStateChangedEventArgs e) {
        this.File?.Save(JobState);
        this.JobStateChanged?.Invoke(this, e);
    }

    public IBackupJobState CreateJobState(IBackupJob backupJob) {
        IBackupJobState backupJobState = new BackupJobState(backupJob);
        JobState.Add(backupJobState);
        backupJobState.JobStateChanged += this.OnJobStateChanged;
        return backupJobState;
    }

    public void Dispose() {
        for (int i = 0; i < JobState.Count; i++) {
            JobState[i].Dispose();
        }

        GC.SuppressFinalize(this);
    }

    public string ToJSON(bool indent = true) {
        return this.File.ToJSON(this.JobState, indent);
    }

    public event JobStateChangedEventHandler? JobStateChanged;
}
