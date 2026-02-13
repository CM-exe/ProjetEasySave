using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;


namespace EasySave.Model;

/// <summary>
/// Data Transfer Object (DTO) representing the state of a backup job.
/// Used for serialization/deserialization of backup job states.
/// </summary>
public class JobStateDto {
    public string Name { get; set; }
    public string SourceFilePath { get; set; }
    public string TargetFilePath { get; set; }
    public double TotalFilesToCopy { get; set; }
    public double TotalFilesSize { get; set; }
    public double NbFilesLeftToDo { get; set; }
    public int Progression { get; set; }
    public string State { get; set; }
}

/// <summary>
/// Interface defining how to persist and load backup job states.
/// </summary>
public interface IStateFile {
    /// <summary>
    /// Saves the current list of backup job states to a file.
    /// </summary>
    public void Save(List<IBackupJobState> jobsState);

    public string Read();

    public string ToJSON(List<IBackupJobState> jobsState, bool indent = true);
}

public class StateFile(string filePath) : IStateFile {
    private string _FilePath { get; set; } = filePath;
    private static object _LockObject { get; } = new();
    private JsonSerializerOptions _SerializerOptions { get; } = new() { WriteIndented = true };

    public void Save(List<IBackupJobState> jobsState) {
        lock (_LockObject) {
            File.WriteAllText(this._FilePath, this.ToJSON(jobsState));
        }
    }

    public string ToJSON(List<IBackupJobState> jobsState, bool indent = true) {
        var dtoList = new List<JobStateDto>();

        foreach (var jobstate in jobsState) {
            var dto = new JobStateDto {
                Name = jobstate.BackupJob?.Name ?? string.Empty,
                SourceFilePath = jobstate.SourceFilePath,
                TargetFilePath = jobstate.DestinationFilePath,
                TotalFilesToCopy = jobstate.TotalFilesToCopy,
                TotalFilesSize = jobstate.TotalFilesSize,
                NbFilesLeftToDo = jobstate.FilesLeft,
                Progression = jobstate.Progression,
                State = jobstate.State.ToString(),
            };
            dtoList.Add(dto);
        }
        return JsonSerializer.Serialize(dtoList, indent ? _SerializerOptions : null);
    }

    public string Read() {
        if (!File.Exists(this._FilePath)) {
            throw new FileNotFoundException($"State file not found: {this._FilePath}");
        }
        lock (_LockObject) {
            return File.ReadAllText(this._FilePath);
        }
    }
}

