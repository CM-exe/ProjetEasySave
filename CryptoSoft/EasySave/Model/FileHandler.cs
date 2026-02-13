 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace EasySave.Model;

public interface IFileHandler : IEntryHandler {
    /// <summary>
    /// Return the extension of the file
    /// </summary>
    /// <returns>The extension of the file</returns>
    public string GetExtension();

    /// <summary>
    /// Write content to the file, if the file does not exist, it will be created, 
    /// if the file exists, it will be overwritten
    /// </summary>
    /// <param name="content">The content to write</param>
    public void Write(string content);

    /// <summary>
    /// Append content to the file, if the file does not exist, it will be created
    /// </summary>
    /// <param name="content">The content to append</param>
    public void Append(string content);

    /// <summary>
    /// Return the last modified date of the file
    /// </summary>
    /// <returns>The last modified date of the file</returns>
    public DateTime GetLastModified();
}

public class FileHandler(string path) : EntryHandler(path), IFileHandler {
    public override string GetName() {
        return Path.GetFileName(this._Path);
    }

    public override double GetSize() {
        if (!this.Exists()) {
            throw new FileNotFoundException("File not found");
        }
        return new FileInfo(this._Path).Length;
    }

    public override void Remove() {
        if (!this.Exists()) {
            throw new FileNotFoundException("File not found");
        }
        File.Delete(this._Path);
    }

    public override void Move(IDirectoryHandler destination, bool forceOverride = false) {
        if (this.Exists()) {
            FileHandler destinationFile = new(Path.Combine(destination.GetPath(), this.GetName()));

            if (destinationFile.Exists() && !forceOverride) {
                throw new IOException("File already exists");
            }

            File.Move(this._Path, destinationFile.GetPath());
            this._Path = destinationFile.GetPath();
        } else {
            throw new FileNotFoundException("File not found");
        }
    }

    public override void Copy(IDirectoryHandler destination, bool forceOverride = false) {
        if (this.Exists()) {
            FileHandler destinationFile = new(Path.Combine(destination.GetPath(), this.GetName()));
            if (destinationFile.Exists() && !forceOverride) {
                throw new IOException("File already exists");
            }
            File.Copy(this._Path, destinationFile.GetPath(), true);
        } else {
            throw new FileNotFoundException("File not found");
        }
    }

    public override void Rename(string newName, bool forceOverride = false) {
        if (this.Exists()) {
            FileHandler destinationFile = new(Path.Combine(Path.GetDirectoryName(this._Path)!, newName));
            if (destinationFile.Exists() && !forceOverride) {
                throw new IOException("File already exists");
            }
            File.Move(this._Path, destinationFile.GetPath());
            this._Path = destinationFile.GetPath();
        } else {
            throw new FileNotFoundException("File not found");
        }
    }

    public override bool Exists() {
        return File.Exists(this._Path);
    }

    public string GetExtension() {
        return Path.GetExtension(this._Path);
    }

    public void Write(string content) {
        using StreamWriter sw = new(this._Path);
        sw.Write(content);
    }

    public void Append(string content) {
        using StreamWriter sw = new(this._Path, true);
        sw.Write(content);
    }

    public override IDirectoryHandler GetParent() {
        return new DirectoryHandler(Path.GetDirectoryName(this._Path)!);
}

    public DateTime GetLastModified() {
        if (!this.Exists()) {
            throw new FileNotFoundException("File not found");
        }
        return File.GetLastWriteTime(this._Path);
    }
}

