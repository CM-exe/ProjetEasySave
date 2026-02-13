using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Model;

public class DifferentialBackupJob(string name, IDirectoryHandler source, IDirectoryHandler destination) : BackupJob(name, source, destination) {
    public override void Analyze() {
        // Perform analysis of the source and destination directories
        // to determine the files that need to be backed up.
        // This class determines the files that need to be backed up.
        // It :
        // - copy non existing and modified files from the source to the destination
        // - remove files from the destination that are not in the source
        // - copy non existing directories from the source to the destination
        this.CompareAndAddTasks(this.Source, this.Destination);
    }

    protected void CompareAndAddTasks(IDirectoryHandler source, IDirectoryHandler destination) {
        // Compare the source and destination directories
        // and add the tasks to the list of tasks to be executed
        List<IEntryHandler> sourceEntries = source.GetEntries();
        foreach (IEntryHandler entry in sourceEntries) {
            if (entry is IFileHandler file) {
                if (destination.Contains(entry)) {
                    if (DifferentialBackupJob.IsFileModified(file, destination.GetFile(entry.GetName()))) {
                        // The file is modified
                        // Add a copy task to the list of tasks
                        this.Tasks.Add(new BackupCopyTask(entry, destination.GetFile(entry.GetName())));
                    }
                } else {
                    // The file does not exist in the destination
                    // Add a copy task to the list of tasks
                    this.Tasks.Add(new BackupCopyTask(entry, destination.GetFile(entry.GetName())));
                }
            } else {
                // The entry is a directory
                // Check if the directory exists in the destination
                if (destination.Contains(entry)) {
                    // The directory exists in the destination
                    // Add a task to compare the directories
                    this.CompareAndAddTasks(source.GetDirectory(entry.GetName()), destination.GetDirectory(entry.GetName()));
                } else {
                    // The directory does not exist in the destination
                    // Add a copy task to the list of tasks
                    this.Tasks.Add(new BackupCopyTask(entry, destination.GetDirectory(entry.GetName())));
                }
            }
        }

        // Check if the destination contains entries that are not in the source
        List<IEntryHandler> destinationEntries = destination.GetEntries();
        foreach (IEntryHandler entry in destinationEntries) {
            if (!source.Contains(entry)) {
                // The entry does not exist in the source
                // Add a remove task to the list of tasks
                this.Tasks.Add(new BackupRemoveTask(null, entry));
            }
        }
    }

    protected static bool IsFileModified(IFileHandler source, IFileHandler destination) {
        // Check if the file is modified
        // This can be done by comparing the size and the last modified date
        return 
            source.GetName() != destination.GetName() ||
            source.GetSize() != destination.GetSize() || 
            source.GetLastModified() != destination.GetLastModified();
    }
}
