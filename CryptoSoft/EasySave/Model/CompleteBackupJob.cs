using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Model;

public class CompleteBackupJob(string name, IDirectoryHandler source, IDirectoryHandler destination) : BackupJob(name, source, destination) {
    public override void Analyze() {
        // Perform analysis of the source and destination directories
        // to determine the files that need to be backed up.
        // This class remove all files in destination and copy all
        // the files from the source to the destination

        if (!this.Source.Exists()) {
            throw new DirectoryNotFoundException($"Source directory '{this.Source.GetPath()}' does not exist.");
        }
        if (!this.Destination.Exists()) {
            throw new DirectoryNotFoundException($"Destination directory '{this.Destination.GetPath()}' does not exist.");
        }

        List<IEntryHandler> destinationEntries = this.Destination.GetEntries();
        foreach (IEntryHandler entry in destinationEntries) {
            this.Tasks.Add(new BackupRemoveTask(null, entry));
        }

        List<IEntryHandler> sourceEntries = this.Source.GetEntries();
        foreach (IEntryHandler entry in sourceEntries) {
            this.Tasks.Add(new BackupCopyTask(entry, 
                entry is IFileHandler ?
                this.Destination.GetFile(entry.GetName()) :
                this.Destination.GetDirectory(entry.GetName())
            ));
        }
    }
}

