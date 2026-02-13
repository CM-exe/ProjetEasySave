using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace EasySave.Model;

public interface IDirectoryHandler : IEntryHandler {
    /// <summary>
    /// Return the list of entries in the directory
    /// </summary>
    /// <returns>The list of entries in the directory</returns>
    /// <exception cref="DirectoryNotFoundException">If the directory does not exist</exception>
    public List<IEntryHandler> GetEntries();

    /// <summary>
    /// Get a file from the directory, this file can be virtual and does not need to exist.
    /// </summary>
    /// <param name="name">The name of the file</param>
    /// <returns>The file handler, this file can be virtual</returns>
    public IFileHandler GetFile(string name);

    /// <summary>
    /// Get a directory from the directory, this directory can be virtual and does not need to exist.
    /// </summary>
    /// <param name="name">The name of the directory</param>
    /// <returns>The directory handler, this directory can be virtual</returns>
    public IDirectoryHandler GetDirectory(string name);

    /// <summary>
    /// Check if the directory contains the entry
    /// </summary>
    /// <param name="entry">The entry to check</param>
    /// <returns>Return true if the directory contains the entry, false otherwise</returns>
    public bool Contains(IEntryHandler entry);
}

public class DirectoryHandler(string path) : EntryHandler(path), IDirectoryHandler {

    public override string GetName() {
        return Path.GetFileName(this._Path);
    }

    public override double GetSize() {
        if (!this.Exists()) {
            throw new DirectoryNotFoundException("Directory not found");
        }
        return new DirectoryInfo(this._Path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length);
    }

    public override void Remove() {
        if (!this.Exists()) {
            throw new DirectoryNotFoundException("Directory not found");
        }
        Directory.Delete(this._Path, true);
    }

    public override void Move(IDirectoryHandler destination, bool forceOverride = false) {
        if (this.Exists()) {
            DirectoryHandler destinationDirectory = new(Path.Combine(destination.GetPath(), this.GetName()));
            if (destinationDirectory.Exists() && !forceOverride) {
                throw new IOException("Directory already exists");
            }
            Directory.Move(this._Path, destinationDirectory.GetPath());
            this._Path = destinationDirectory.GetPath();
        } else {
            throw new DirectoryNotFoundException("Directory not found");
        }
    }

    public override void Copy(IDirectoryHandler destination, bool forceOverride = false) {
        if (this.Exists()) {
            DirectoryHandler destinationDirectory = new(Path.Combine(destination.GetPath(), this.GetName()));
            if (destinationDirectory.Exists() && !forceOverride) {
                throw new IOException("Directory already exists");
            }
            Directory.CreateDirectory(destinationDirectory.GetPath());
            foreach (var file in Directory.GetFiles(this._Path)) {
                File.Copy(file, Path.Combine(destinationDirectory.GetPath(), Path.GetFileName(file)), forceOverride);
            }
        } else {
            throw new DirectoryNotFoundException("Directory not found");
        }
    }

    public override void Rename(string newName, bool forceOverride = false) {
        if (this.Exists()) {
            DirectoryHandler destinationDirectory = new(Path.Combine(this.GetParent().GetName(), newName));
            if (destinationDirectory.Exists() && !forceOverride) {
                throw new IOException("Directory already exists");
            }
            Directory.Move(this._Path, destinationDirectory.GetPath());
            this._Path = destinationDirectory.GetPath();
        } else {
            throw new DirectoryNotFoundException("Directory not found");
        }
    }

    public override bool Exists() {
        return Directory.Exists(this._Path);
    }

    public List<IEntryHandler> GetEntries() {
        if (!this.Exists()) {
            throw new DirectoryNotFoundException("Directory not found");
        }
        return [.. Directory.GetFileSystemEntries(this._Path).Select(entry => {
            if (Directory.Exists(entry)) {
                return new DirectoryHandler(entry) as IEntryHandler;
            } else {
                return new FileHandler(entry) as IEntryHandler;
            }
        })];
    }

    public override IDirectoryHandler GetParent() {
        if (!this.Exists()) {
            throw new DirectoryNotFoundException("Directory not found");
        }

        string parentName = (Directory.GetParent(this._Path)?.FullName) ?? throw new DirectoryNotFoundException("Parent directory not found");

        return new DirectoryHandler(parentName);
    }

    public IFileHandler GetFile(string name) {
        string filePath = Path.Combine(this._Path, name);
        return new FileHandler(filePath);
    }

    public IDirectoryHandler GetDirectory(string name) {
        string directoryPath = Path.Combine(this._Path, name);
        return new DirectoryHandler(directoryPath);
}

    public bool Contains(IEntryHandler entry) {
        if (!this.Exists()) {
            throw new DirectoryNotFoundException("Directory not found");
        }
        return this.GetEntries().Any(e => e.GetName() == entry.GetName());
    }
}

