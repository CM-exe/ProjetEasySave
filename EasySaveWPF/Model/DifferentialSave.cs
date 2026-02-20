using CryptoSoft;
using EasyLog;
using ProjetEasySave.Utils;
using System.Diagnostics;
using System.IO;

namespace ProjetEasySave.Model
{
    public class DifferentialSave : ISaveStrategy
    {
        /*
         * Differential Save implementation of the ISaveStrategy interface.
         * This strategy only saves files that have changed since the last full backup.
         */

        // Interface attributes
        private static Config _config = Config.Instance; // Load config for business software checking
        private static SaveTaskState _state = SaveTaskState.PENDING; // State for the task

        // Attributes
        private Logger _logger = Logger.getInstance(Config.Instance);
        private string _fullBackupPath; // Reference path of the full backup
        private SaveTask _saveTask;

        // Constructor
        public DifferentialSave(SaveTask saveTask, string fullBackupPath)
        {
            _saveTask = saveTask;
            _fullBackupPath = fullBackupPath;
        }

        // Methods
        public string getCompleteSavePath()
        {
            return _fullBackupPath;
        }

        // Instance method to check if the business software is running
        public bool isBusinessSoftwareRunning()
        {
            // Name of the process to look for
            string businessSoftwareName = _config.getBusinessSoftwareName();

            // Fetch all processes matching the specified name
            Process[] processes = Process.GetProcessesByName(businessSoftwareName);

            // Return true if at least one instance is actively running
            return processes.Length > 0;
        }

        public void waitForBusinessSoftwareToClose()
        {
            string businessSoftwareName = _config.getBusinessSoftwareName();
            Process[] processes = Process.GetProcessesByName(businessSoftwareName);
            if (processes.Length > 0)
            {
                _logger.log(Logger.formatInfoRealTimeMessage(
                    "Business software detected, waiting for it to close...",
                    "DifferentialSave",
                    "",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    SaveTaskState.WAITING
                ));
                setState(SaveTaskState.WAITING);
                foreach (Process process in processes)
                {
                    process.WaitForExit();
                }
                
            }
            setState(SaveTaskState.RUNNING);
        }

        // Update the state of the save task
        public SaveTaskState setState(SaveTaskState state)
        {
            _state = state;
            _saveTask.onSaveTaskStateUpdated(this);
            return state;
        }

        // Getter for state
        public SaveTaskState getState()
        {
            return _state; 
        }

        // doSave method implementation for Differential Save
        public bool doSave(string sourcePath, string destinationPath)
        {
            try
            {
                // Validate paths
                if (string.IsNullOrWhiteSpace(sourcePath) ||
                    string.IsNullOrWhiteSpace(destinationPath) ||
                    string.IsNullOrWhiteSpace(_fullBackupPath))
                {
                    _logger.log(Logger.formatErrMessage("Source, destination, or full backup path is invalid."));
                    setState(SaveTaskState.FAILED);
                    return false;
                }

                if (!Directory.Exists(sourcePath))
                {
                    _logger.log(Logger.formatErrMessage($"Source path '{sourcePath}' does not exist."));
                    setState(SaveTaskState.FAILED);
                    return false;
                }

                if (!Directory.Exists(_fullBackupPath))
                {
                    _logger.log(Logger.formatErrMessage($"Full backup path '{_fullBackupPath}' does not exist."));
                    setState(SaveTaskState.FAILED);
                    return false;
                }

                // Check a first time if business software is running before starting the save process
                if (isBusinessSoftwareRunning())
                {
                    waitForBusinessSoftwareToClose();
                }

                // Get global encryption settings from Config Singleton
                string cryptoKey = Config.Instance.getEncryptionKey();
                List<string> cryptoExtensions = Config.Instance.getEncryptionExtensions();

                _logger.log(Logger.formatLogMessage(
                    "Differential Save Started",
                    sourcePath,
                    destinationPath,
                    0,
                    0,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                ));

                // Initialize destination directory
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    // Clean the destination directory before starting
                    foreach (var file in Directory.EnumerateFiles(destinationPath, "*", SearchOption.AllDirectories))
                    {
                        File.Delete(file);
                    }
                }

                // Main loop: iterate through all files in the source directory and apply differential logic
                foreach (var sourceFile in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
                {
                    if (isBusinessSoftwareRunning())
                    {
                        waitForBusinessSoftwareToClose();
                    }

                    var relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                    var fullFile = Path.Combine(_fullBackupPath, relativePath);
                    var diffFile = Path.Combine(destinationPath, relativePath);

                    bool shouldCopy = false;

                    // Differential logic: check if file is new or modified
                    if (!File.Exists(fullFile))
                    {
                        shouldCopy = true;
                    }
                    else
                    {
                        var sourceDate = File.GetLastWriteTime(sourceFile);
                        var fullDate = File.GetLastWriteTime(fullFile);

                        if (sourceDate > fullDate)
                        {
                            shouldCopy = true;
                        }
                    }

                    if (shouldCopy)
                    {
                        // Ensure the subdirectory exists in the destination
                        Directory.CreateDirectory(Path.GetDirectoryName(diffFile)!);

                        FileInfo fileInfo = new FileInfo(sourceFile);
                        string extension = Path.GetExtension(sourceFile);

                        // Information: 0 (no encryption), >0 (time in ms), <0 (error code)
                        double encryptionDuration = 0;
                        DateTime startTime = DateTime.Now;

                        // Encryption Decision 
                        bool needEncryption = !string.IsNullOrEmpty(cryptoKey)
                                              && cryptoExtensions != null
                                              && cryptoExtensions.Contains(extension);

                        if (needEncryption)
                        {
                            // Call the CryptoSoft DLL method
                            encryptionDuration = FileManager.CryptFile(sourceFile, diffFile, cryptoKey);
                        }
                        else
                        {
                            // Standard file copy
                            File.Copy(sourceFile, diffFile, true);
                        }

                        // Logging 
                        _logger.log(Logger.formatLogMessage(
                            needEncryption ? "Copying File (Differential + Encrypted)" : "Copying File (Differential)",
                            sourceFile,
                            diffFile,
                            (int)fileInfo.Length,
                            encryptionDuration, // 0 if copy, ms if encrypted
                            startTime.ToString("yyyy-MM-dd HH:mm:ss")
                        ));

                        // Abort if the encryption/copy process failed (duration < 0)
                        if (encryptionDuration < 0) return false;
                    }
                }

                // "Save completed" log
                _logger.log(Logger.formatCompleteSaveMessage(
                    "Differential Save Finished",
                    sourcePath,
                    destinationPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                ));
                setState(SaveTaskState.COMPLETED);

                return true;
            }
            catch (Exception ex)
            {
                _logger.log(Logger.formatErrMessage($"An error occurred during differential save: {ex.Message}"));
                setState(SaveTaskState.FAILED);
                return false;
            }
        }

        // Overloaded doSave method with cancellation and progress reporting
        public bool doSave(
            string source,
            string destination,
            CancellationToken token,
            ManualResetEventSlim pauseEvent,
            Action<int, string> progress)
        {
            try
            {
                _state = SaveTaskState.RUNNING;

                var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories)
                    .Where(f => IsNewOrModified(f, source))
                    .ToArray();

                long totalBytes = files.Sum(f => new FileInfo(f).Length);
                long copiedBytes = 0;

                foreach (var file in files)
                {
                    token.ThrowIfCancellationRequested();
                    pauseEvent.Wait();

                    string relative = Path.GetRelativePath(source, file);
                    string target = Path.Combine(destination, relative);

                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);

                    CopyFile(
                        file,
                        target,
                        ref copiedBytes,
                        totalBytes,
                        token,
                        pauseEvent,
                        progress
                    );
                }

                _state = SaveTaskState.COMPLETED;
                return true;
            }
            catch (OperationCanceledException)
            {
                _state = SaveTaskState.STOPPED;
                throw;
            }
            catch
            {
                _state = SaveTaskState.FAILED;
                return false;
            }
        }

        // File copy with progress
        private void CopyFile(
            string source,
            string destination,
            ref long copiedBytes,
            long totalBytes,
            CancellationToken token,
            ManualResetEventSlim pauseEvent,
            Action<int, string> progress)
        {
            const int bufferSize = 81920;
            byte[] buffer = new byte[bufferSize];

            using var input = File.OpenRead(source);
            using var output = File.Create(destination);

            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                token.ThrowIfCancellationRequested();
                pauseEvent.Wait();

                output.Write(buffer, 0, read);
                copiedBytes += read;

                int percent = totalBytes == 0
                    ? 100
                    : (int)(copiedBytes * 100 / totalBytes);

                progress?.Invoke(percent, Path.GetFileName(source));
            }
        }

        // Differential logic 
        private bool IsNewOrModified(string file, string sourceRoot)
        {
            string relative = Path.GetRelativePath(sourceRoot, file);
            string completeFile = Path.Combine(_fullBackupPath, relative);

            if (!File.Exists(completeFile))
                return true;

            DateTime srcDate = File.GetLastWriteTimeUtc(file);
            DateTime completeDate = File.GetLastWriteTimeUtc(completeFile);

            return srcDate > completeDate;
        }
    }
}