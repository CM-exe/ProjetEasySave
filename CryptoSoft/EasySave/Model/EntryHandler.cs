using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace EasySave.Model;

public interface IEntryHandler {
    /// <summary>
    /// Return the name of the file or directory
    /// </summary>
    /// <returns>The name of the file or directory</returns>
    public string GetName();

    /// <summary>
    /// Return the path of the file or directory
    /// </summary>
    /// <returns>The path of the file or directory</returns>
    public string GetPath();

    /// <summary>
    /// Return the size of the file or directory
    /// </summary>
    /// <exception cref="FileNotFoundException">If the file or directory does not exist</exception>
    /// <returns>The size in bytes</returns>
    public double GetSize();

    /// <summary>
    /// Remove the file or directory
    /// </summary>
    /// <exception cref="FileNotFoundException">If the file or directory does not exist</exception>
    public void Remove();

    /// <summary>
    /// Move the file or directory to a new destination
    /// </summary>
    /// <param name="destination">The destination directory handler</param>
    /// <param name="forceOverride">If true, override the existing file or directory</param>
    /// <exception cref="IOException">If the file or directory already exists and forceOverride is false</exception>
    /// <exception cref="FileNotFoundException">If the file or directory does not exist</exception>
    public void Move(IDirectoryHandler destination, bool forceOverride = false);

    /// <summary>
    /// Copy the file or directory to a new destination
    /// </summary
    /// <param name="destination">The destination directory handler</param>
    /// <param name="forceOverride">If true, override the existing file or directory</param>
    /// <exception cref="IOException">If the file or directory already exists and forceOverride is false</exception>
    /// <exception cref="FileNotFoundException">If the file or directory does not exist</exception>
    public void Copy(IDirectoryHandler destination, bool forceOverride = false);

    /// <summary>
    /// Rename the file or directory
    /// </summary>
    /// <param name="newName">The new name of the file or directory</param>
    /// <param name="forceOverride">If true, override the existing file or directory</param>
    /// <exception cref="IOException">If the file or directory already exists and forceOverride is false</exception>
    /// <exception cref="FileNotFoundException">If the file or directory does not exist</exception>
    public void Rename(string newName, bool forceOverride = false);

    /// <summary>
    /// Check if the file or directory exists
    /// </summary>
    /// <returns>True if the file or directory exists, false otherwise</returns>
    public bool Exists();

    /// <summary>
    /// Return the parent directory of the current directory or file
    /// </summary>
    public IDirectoryHandler GetParent();
}

public abstract class EntryHandler(string path) : IEntryHandler {
    protected string _Path = path;

    public string GetPath() {
        return _Path;
}

    public abstract string GetName();
    
    public abstract double GetSize();

    public abstract void Remove();

    public abstract void Move(IDirectoryHandler destination, bool forceOverride = false);

    public abstract void Copy(IDirectoryHandler destination, bool forceOverride = false);

    public abstract void Rename(string newName, bool forceOverride = false);

    public abstract bool Exists();

    public abstract IDirectoryHandler GetParent();

    public override string ToString() {
        return this.GetPath();
    }
}
