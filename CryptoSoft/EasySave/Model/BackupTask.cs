using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Model;

public interface IBackupTask {
    /// <summary>
    /// Source file or directory to be backed up.
    /// </summary>
    public IEntryHandler? Source { get; }
    /// <summary>
    /// Destination file or directory where the backup will be stored.
    /// </summary>
    public IEntryHandler? Destination { get; }
    /// <summary>
    /// Start time of the backup task.
    /// </summary>
    public DateTime? StartTime { get; set; }
    /// <summary>
    /// End time of the backup task.
    /// </summary>
    public DateTime? EndTime { get; set; }

    public bool IsRemoveTask { get; }

    public double CryptDuration { get; set; }

    /// <summary>
    /// Duration of the backup task in milliseconds.
    /// </summary>
    /// <returns>Duration in milliseconds.</returns>
    public double GetDuration();
    /// <summary>
    /// Run the backup task.
    /// </summary>
    public void Run();
} 

public abstract class BackupTask(IEntryHandler? source, IEntryHandler? destination) : IBackupTask {
    public IEntryHandler? Source {
        get => source;
    }
    public IEntryHandler? Destination {
        get => destination;
    }

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double CryptDuration { get; set; }

    public bool IsRemoveTask => false;

    public double GetDuration() {
        if (StartTime == null || EndTime == null) {
            return 0;
        }
        return (EndTime - StartTime).Value.TotalMilliseconds;
    }

    public void Run() {
        this.StartTime = DateTime.Now;
        this.Algorithm();
        this.EndTime = DateTime.Now;
    }

    protected abstract void Algorithm();
} 

class BackupVoidTask : BackupTask {
    public BackupVoidTask() : base(null, null) { }

    protected override void Algorithm() {
        // No operation
    }
}