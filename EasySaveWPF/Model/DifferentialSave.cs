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
        private SemaphoreSlim _bigFileSemaphore; // Semaphore for big file handling
        private Queue<string> _pendingFiles; // Queue to manage big file save requests

        // Constructor
        public DifferentialSave(SaveTask saveTask, SemaphoreSlim bigFileSempahore, string fullBackupPath)
        {
            _saveTask = saveTask;
            _bigFileSemaphore = bigFileSempahore;
            _fullBackupPath = fullBackupPath;
            _pendingFiles = new Queue<string>();
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

        private int processFile(
    string sourceFile,
    string sourcePath,
    string destinationPath,
    string fullBackupPath,
    string cryptoKey,
    List<string> cryptoExtensions,
    long totalBytes,
    ref long copiedBytes,
    Action<int, string> progress,
    CancellationToken token,
    ManualResetEventSlim pauseEvent)
        {
            var relativePath = Path.GetRelativePath(sourcePath, sourceFile);
            var diffFile = Path.Combine(destinationPath, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(diffFile)!);

            FileInfo fileInfo = new FileInfo(sourceFile);
            string extension = Path.GetExtension(sourceFile);

            // Information: 0 (no encryption), >0 (time in ms), <0 (error code)
            double encryptionDuration = 0;
            DateTime startTime = DateTime.Now;

            bool needEncryption = !string.IsNullOrEmpty(cryptoKey)
                                  && cryptoExtensions != null
                                  && cryptoExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase));

            // Update UI
            int currentPercentage = totalBytes > 0 ? (int)((copiedBytes * 100) / totalBytes) : 0;
            progress?.Invoke(currentPercentage, fileInfo.Name);

            if (needEncryption)
            {
                encryptionDuration = FileManager.CryptFile(sourceFile, diffFile, cryptoKey);
                if (encryptionDuration < 0)
                {
                    _logger.log(Logger.formatErrMessage($"Error encrypting file: {sourceFile}"));
                    return -1;
                }

                copiedBytes += fileInfo.Length;
                currentPercentage = totalBytes > 0 ? (int)((copiedBytes * 100) / totalBytes) : 100;
                progress?.Invoke(currentPercentage, fileInfo.Name);
            }
            else
            {
                using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                using (FileStream destinationStream = new FileStream(diffFile, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[81920]; 
                    int bytesRead;

                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        token.ThrowIfCancellationRequested();
                        pauseEvent.Wait(token);

                        destinationStream.Write(buffer, 0, bytesRead);
                        copiedBytes += bytesRead;

                        // Update UI
                        currentPercentage = totalBytes > 0 ? (int)((copiedBytes * 100) / totalBytes) : 100;
                        progress?.Invoke(currentPercentage, fileInfo.Name);
                    }
                }
            }

            _logger.log(Logger.formatLogMessage(
                needEncryption ? "Copying File (Differential + Encrypted)" : "Copying File (Differential)",
                sourceFile,
                diffFile,
                (int)fileInfo.Length,
                encryptionDuration,
                startTime.ToString("yyyy-MM-dd HH:mm:ss")
            ));

            return (int)encryptionDuration;
        }

        public bool doSave(
            string sourcePath,
            string destinationPath,
            List<string> priorityExt,
            CancellationToken token,
            ManualResetEventSlim pauseEvent,
            Action<int, string> progress)
        {
            try
            {
                // Set initial state
                setState(SaveTaskState.RUNNING);

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

                if (isBusinessSoftwareRunning())
                {
                    waitForBusinessSoftwareToClose();
                }

                string cryptoKey = Config.Instance.getEncryptionKey();
                List<string> cryptoExtensions = Config.Instance.getEncryptionExtensions();

                _logger.log(Logger.formatLogMessage("Differential Save Started", sourcePath, destinationPath, 0, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    foreach (var file in Directory.EnumerateFiles(destinationPath, "*", SearchOption.AllDirectories))
                    {
                        token.ThrowIfCancellationRequested(); 
                        File.Delete(file);
                    }
                }

                var allFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                var filesToProcess = new List<string>();

                foreach (var file in allFiles)
                {
                    var relativePath = Path.GetRelativePath(sourcePath, file);
                    var fullFile = Path.Combine(_fullBackupPath, relativePath);

                    if (!File.Exists(fullFile) || File.GetLastWriteTime(file) > File.GetLastWriteTime(fullFile))
                    {
                        filesToProcess.Add(file);
                    }
                }

                long totalBytes = filesToProcess.Sum(f => new FileInfo(f).Length);
                long copiedBytes = 0; 

                var sortedFiles = filesToProcess.OrderBy(f =>
                {
                    string ext = Path.GetExtension(f);
                    int index = priorityExt.FindIndex(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
                    return index >= 0 ? index : int.MaxValue;
                }).ToArray();

                // Main File Loop
                foreach (var sourceFile in sortedFiles)
                {
                    token.ThrowIfCancellationRequested();
                    pauseEvent.Wait(token);

                    if (isBusinessSoftwareRunning())
                    {
                        waitForBusinessSoftwareToClose();
                    }

                    if (_pendingFiles.Count > 0 && _bigFileSemaphore.CurrentCount > 0)
                    {
                        while (_pendingFiles.Count > 0)
                        {
                            token.ThrowIfCancellationRequested();
                            pauseEvent.Wait(token);

                            _bigFileSemaphore.Wait(token);
                            string pendingFile = _pendingFiles.Dequeue();
                            int local_encryptionDuration = processFile(pendingFile, sourcePath, destinationPath, _fullBackupPath, cryptoKey, cryptoExtensions, totalBytes, ref copiedBytes, progress, token, pauseEvent);
                            _bigFileSemaphore.Release();

                            if (local_encryptionDuration < 0) return false;
                        }
                    }

                    FileInfo fileInfo = new FileInfo(sourceFile);

                    if (fileInfo.Length > (_config.getBiggestSize() * 1000) && _bigFileSemaphore.CurrentCount == 0)
                    {
                        _pendingFiles.Enqueue(sourceFile);
                        continue;
                    }
                    else if (fileInfo.Length > (_config.getBiggestSize() * 1000) && _bigFileSemaphore.CurrentCount > 0)
                    {
                        _bigFileSemaphore.Wait(token);
                        int local_encryptionDuration = processFile(sourceFile, sourcePath, destinationPath, _fullBackupPath, cryptoKey, cryptoExtensions, totalBytes, ref copiedBytes, progress, token, pauseEvent);
                        _bigFileSemaphore.Release();

                        if (local_encryptionDuration < 0) return false;
                        continue;
                    }
                    else
                    {
                        int encryptionDuration = processFile(sourceFile, sourcePath, destinationPath, _fullBackupPath, cryptoKey, cryptoExtensions, totalBytes, ref copiedBytes, progress, token, pauseEvent);
                        if (encryptionDuration < 0) return false;
                        continue;
                    }
                }
                
                while (_pendingFiles.Count > 0)
                {
                    token.ThrowIfCancellationRequested();
                    pauseEvent.Wait(token);

                    _bigFileSemaphore.Wait(token);
                    string pendingFile = _pendingFiles.Dequeue();
                    int local_encryptionDuration = processFile(pendingFile, sourcePath, destinationPath, _fullBackupPath, cryptoKey, cryptoExtensions, totalBytes, ref copiedBytes, progress, token, pauseEvent);
                    _bigFileSemaphore.Release();

                    if (local_encryptionDuration < 0) return false;
                }

                _logger.log(Logger.formatCompleteSaveMessage("Differential Save Finished", sourcePath, destinationPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                setState(SaveTaskState.COMPLETED);

                return true;
            }
            catch (OperationCanceledException)
            {
                setState(SaveTaskState.STOPPED);
                throw;
            }
            catch (Exception ex)
            {
                _logger.log(Logger.formatErrMessage($"An error occurred during differential save: {ex.Message}"));
                setState(SaveTaskState.FAILED);
                return false;
            }
        }
    }
}