using CryptoSoft;
using EasyLog;
using ProjetEasySave.Utils;
using ProjetEasySave.ViewModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ProjetEasySave.Model
{
    public class CompleteSave : ISaveStrategy
    {
        /*
         * Complete Save implementation of the ISaveStrategy interface.
         */

        // Interface attributes
        private static Config config = Config.Instance; // Load config for business software checking
        private static SaveTaskState _state = SaveTaskState.PENDING; // State for task

        // Attributes
        private Logger _logger = Logger.getInstance(Config.Instance);
        private SaveTask _saveTask; // Reference to the SaveTask for state updates (if needed)
        private SemaphoreSlim _bigFileSemaphore; // Semaphore for big file handling
        private Queue<string> _pendingFiles; // Queue to manage pending files when big file is being processed

        // Constructor
        public CompleteSave(SaveTask saveTask, SemaphoreSlim bigFileSemaphore)
        {
            _saveTask = saveTask;
            _bigFileSemaphore = bigFileSemaphore;
            _pendingFiles = new Queue<string>();
        }

        // Instance method to check if the business software is running
        public bool isBusinessSoftwareRunning()
        {
            // Name of the process to look for
            string businessSoftwareName = config.getBusinessSoftwareName();

            // Fetch all processes matching the specified name
            Process[] processes = Process.GetProcessesByName(businessSoftwareName);

            // Return true if at least one instance is actively running
            return processes.Length > 0;
        }

        public void waitForBusinessSoftwareToClose()
        {
            string businessSoftwareName = config.getBusinessSoftwareName();
            Process[] processes = Process.GetProcessesByName(businessSoftwareName);
            if (processes.Length > 0)
            {
                _logger.log(Logger.formatInfoRealTimeMessage(
                    "Business software detected, waiting for it to close...",
                    "CompleteSave",
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
    string file,
    string sourcePath,
    string destinationPath,
    string cryptoKey,
    List<string> cryptoExtensions,
    long totalBytes,
    ref long copiedBytes,
    CancellationToken token,
    ManualResetEventSlim pauseEvent,
    Action<int, string> progress)
        {
            var relative = Path.GetRelativePath(sourcePath, file);
            var targetFile = Path.Combine(destinationPath, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            FileInfo fileInfo = new FileInfo(file);
            string extension = Path.GetExtension(file);

            // Information: 0 (no encryption), >0 (time in ms), <0 (error code)
            double encryptionDuration = 0;
            DateTime startTime = DateTime.Now;

            bool shouldEncrypt = !string.IsNullOrEmpty(cryptoKey)
                                 && cryptoExtensions != null
                                 && cryptoExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase));

            int currentPercentage = totalBytes > 0 ? (int)((copiedBytes * 100) / totalBytes) : 0;
            progress?.Invoke(currentPercentage, fileInfo.Name);

            if (shouldEncrypt)
            {
                encryptionDuration = FileManager.CryptFile(file, targetFile, cryptoKey);
                if (encryptionDuration < 0)
                {
                    _logger.log(Logger.formatErrMessage($"Error encrypting file: {file}"));
                    return -1;
                }

                copiedBytes += fileInfo.Length;
                currentPercentage = totalBytes > 0 ? (int)((copiedBytes * 100) / totalBytes) : 100;
                progress?.Invoke(currentPercentage, fileInfo.Name);
            }
            else
            {
                using (FileStream sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                using (FileStream destinationStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write))
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
                shouldEncrypt ? "Copying File (Encrypted)" : "Copying File",
                file,
                targetFile,
                (int)fileInfo.Length,
                encryptionDuration,
                startTime.ToString("yyyy-MM-dd HH:mm:ss")
            ));

            return (int)encryptionDuration;
        }

        // Interface method implementation

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
                setState(SaveTaskState.RUNNING);

                _pendingFiles.Clear();

                if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath))
                {
                    _logger.log(Logger.formatErrMessage("Source or destination path is invalid."));
                    setState(SaveTaskState.FAILED);
                    return false;
                }

                if (!Directory.Exists(sourcePath))
                {
                    _logger.log(Logger.formatErrMessage($"Source path does not exist: {sourcePath}"));
                    setState(SaveTaskState.FAILED);
                    return false;
                }

                if (isBusinessSoftwareRunning())
                {
                    waitForBusinessSoftwareToClose();
                }

                _logger.log(Logger.formatLogMessage("Complete Save Started", sourcePath, destinationPath, 0, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                string cryptoKey = Config.Instance.getEncryptionKey();
                List<string> cryptoExtensions = Config.Instance.getEncryptionExtensions();

                if (!Directory.Exists(destinationPath))
                {
                    foreach (var dir in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                    {
                        var relative = Path.GetRelativePath(sourcePath, dir);
                        var targetDir = Path.Combine(destinationPath, relative);
                        Directory.CreateDirectory(targetDir);
                    }
                }
                else
                {
                    foreach (var file in Directory.EnumerateFiles(destinationPath, "*", SearchOption.AllDirectories))
                    {
                        token.ThrowIfCancellationRequested(); 
                        File.Delete(file);
                    }
                }

                string[] files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                long totalBytes = files.Sum(f => new FileInfo(f).Length);
                long copiedBytes = 0;

                files = files.OrderBy(f =>
                {
                    string ext = Path.GetExtension(f);
                    int index = priorityExt.FindIndex(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
                    return index >= 0 ? index : int.MaxValue;
                }).ToArray();

                // Main File Loop
                foreach (var file in files)
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
                            try
                            {
                                int local_encryptionDuration = processFile(pendingFile, sourcePath, destinationPath, cryptoKey, cryptoExtensions, totalBytes, ref copiedBytes, token, pauseEvent, progress);
                                if (local_encryptionDuration < 0) return false;
                            }
                            finally
                            {
                            _bigFileSemaphore.Release();
                            }
                        }
                    }

                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.Length > (config.getBiggestSize() * 1000) && _bigFileSemaphore.CurrentCount == 0)
                    {
                        _pendingFiles.Enqueue(file);
                        continue;
                    }
                    else if (fileInfo.Length > (config.getBiggestSize() * 1000) && _bigFileSemaphore.CurrentCount > 0)
                    {
                        _bigFileSemaphore.Wait(token);
                        try
                        {
                            int local_encryptionDuration = processFile(file, sourcePath, destinationPath, cryptoKey, cryptoExtensions, totalBytes, ref copiedBytes, token, pauseEvent, progress);
                            if (local_encryptionDuration < 0) return false;
                        }
                        finally
                        {
                            _bigFileSemaphore.Release();
                        }
                        continue;
                    }
                    else
                    {
                        int encryptionDuration = processFile(file, sourcePath, destinationPath, cryptoKey, cryptoExtensions, totalBytes, ref copiedBytes, token, pauseEvent, progress);
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
                    try
                    {
                        int local_encryptionDuration = processFile(pendingFile, sourcePath, destinationPath, cryptoKey, cryptoExtensions, totalBytes, ref copiedBytes, token, pauseEvent, progress);
                        if (local_encryptionDuration < 0) return false;
                    }
                    finally
                    {
                        _bigFileSemaphore.Release();
                    }
                }

                _logger.log(Logger.formatCompleteSaveMessage("Complete Save Finished", sourcePath, destinationPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
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
                _logger.log(Logger.formatErrMessage($"Error during complete save: {ex.Message}"));
                setState(SaveTaskState.FAILED);
                return false;
            }
        }
    }
}