using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EasyRemote.Model;


public interface IBackupJobState {
    public string Name { get; set; }
    public string SourceFilePath { get; set; }
    public string DestinationFilePath { get; set; }
    public string State { get; set; }
    public double TotalFilesToCopy { get; set; }
    public double TotalFileSize { get; set; }
    public int Progression { get; set; }

}

class BackupJobState : IBackupJobState {
    public required string Name { get; set; }
    public string SourceFilePath { get; set; } = string.Empty;
    public string DestinationFilePath { get; set; } = string.Empty;
    public required string State { get; set; }
    public double TotalFilesToCopy { get; set; } = 0;
    public double TotalFileSize { get; set; } = 0;
    public int Progression { get; set; } = 0;

    [JsonIgnore]
    public IBackupJob? BackupJob { get; set; }

    [JsonIgnore]
    public bool IsPaused => State == "PAUSED";
}

