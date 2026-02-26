using CryptoSoft;
using EasyLog;
using ProjetEasySave.Utils;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace ProjetEasySave.Model
{
    /// <summary>
    /// Represents the differential backup strategy implementation of the <see cref="ISaveStrategy"/> interface.
    /// </summary>
    /// <remarks>
    /// This strategy compares the source directory against a previously completed full backup. 
    /// It only copies files that are new or have been modified since that full backup. 
    /// It handles large files, encryption, and pauses when restricted business software is running.
    /// </remarks>
    public class DifferentialSave : ISaveStrategy
    {
        // Interface attributes
        private static Config _config = Config.Instance; // Load config for business software checking
        private static SaveTaskState _state = SaveTaskState.PENDING; // State for the task

        // Attributes
        private Logger _logger = Logger.getInstance(Config.Instance);
        private string _fullBackupPath; // Reference path of the full backup
        private SaveTask _saveTask;
        private SemaphoreSlim _bigFileSemaphore; // Semaphore for big file handling
        private Queue<string> _pendingFiles; // Queue to manage big file save requests

        /// <summary>
        /// Initializes a new instance of the <see cref="DifferentialSave"/> class.
        /// </summary>
        /// <param name="saveTask">The main task object associated with this backup operation.</param>
        /// <param name="bigFileSemaphore">The semaphore used to limit concurrent transfers of large files across the application.</param>
        /// <param name="fullBackupPath">The reference path to the last complete backup used for comparison.</param>
        public DifferentialSave(SaveTask saveTask, SemaphoreSlim bigFileSemaphore, string fullBackupPath)
        {
            _saveTask = saveTask;
            _bigFileSemaphore = bigFileSemaphore;
            _fullBackupPath = fullBackupPath;
            _pendingFiles = new Queue<string>();
        }

        /// <summary>
        /// Retrieves the reference path of the complete backup used by this differential strategy.
        /// </summary>
        /// <returns>A string representing the path to the full backup directory.</returns>
        public string getCompleteSavePath()
        {
            return _fullBackupPath;
        }

        /// <summary>
        /// Checks whether the configured business software is currently actively running on the system.
        /// </summary>
        /// <returns><c>true</c> if the business software is running; otherwise, <c>false</c>.</returns>
        public bool isBusinessSoftwareRunning()
        {
            // Name of the process to look for
            string businessSoftwareName = _config.getBusinessSoftwareName();

            // Fetch all processes matching the specified name
            Process[] processes = Process.GetProcessesByName(businessSoftwareName);

            // Return true if at least one instance is actively running
            return processes.Length > 0;
        }

        /// <summary>
        /// Suspends the backup thread and waits until all instances of the conflicting business software are closed.
        /// Logs a waiting message to the real-time tracker.
        /// </summary>
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

        /// <summary>
        /// Updates the execution state of the backup task and triggers the notification event in the main task.
        /// </summary>
        /// <param name="state">The new <see cref="SaveTaskState"/> to apply.</param>
        /// <returns>The updated state.</returns>
        public SaveTaskState setState(SaveTaskState state)
        {
            _state = state;
            _saveTask.onSaveTaskStateUpdated(this);
            return state;
        }

        /// <summary>
        /// Retrieves the current execution state of the backup task.
        /// </summary>
        /// <returns>The current <see cref="SaveTaskState"/>.</returns>
        public SaveTaskState getState()
        {
            return _state;
        }

        /// <summary>
        /// Processes a single file by copying it to the destination and applying encryption if required.
        /// </summary>
        /// <param name="sourceFile">The full path of the source file to copy.</param>
        /// <param name="sourcePath">The root directory path of the backup source.</param>
        /// <param name="destinationPath">The root directory path of the backup destination.</param>
        /// <param name="fullBackupPath">The root directory path of the full backup reference.</param>
        /// <param name="cryptoKey">The key used for file encryption.</param>
        /// <param name="cryptoExtensions">A list of file extensions that require encryption.</param>
        /// <param name="totalBytes">The total size in bytes of all files that need to be copied.</param>
        /// <param name="copiedBytes">The total number of bytes successfully copied so far (passed by reference).</param>
        /// <param name="progress">A delegate used to report the progress percentage and current file name to the UI.</param>
        /// <param name="token">A cancellation token to observe for stop requests mid-copy.</param>
        /// <param name="pauseEvent">A reset event used to pause and resume the copying process.</param>
        /// <returns>The duration of the encryption process in milliseconds, <c>0</c> if no encryption occurred, or <c>-1</c> if an error happened.</returns>
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
            ManualResetEventSlim pauseEvent
        )
        {
            var relativePath = Path.GetRelativePath(sourcePath, sourceFile);
            var diffFile = Path.Combine(destinationPath, relativePath);

            if (isBusinessSoftwareRunning())
            {
                waitForBusinessSoftwareToClose();
            }

            // Ensure the subdirectory exists in the destination
            Directory.CreateDirectory(Path.GetDirectoryName(diffFile)!);

            FileInfo fileInfo = new FileInfo(sourceFile);
            string extension = Path.GetExtension(sourceFile);

            // Information: 0 (no encryption), >0 (time in ms), <0 (error code)
            double encryptionDuration = 0;
            DateTime startTime = DateTime.Now;

            bool needEncryption = !string.IsNullOrEmpty(cryptoKey)
                                  && cryptoExtensions != null
                                  && cryptoExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase));

            // Update UI with the current file name before starting
            int currentPercentage = totalBytes > 0 ? (int)((copiedBytes * 100) / totalBytes) : 0;
            progress?.Invoke(currentPercentage, fileInfo.Name);

            if (needEncryption)
            {
                // Check Cryptosoft is mono instance and wait if necessary
                if (!FileManager.IsSingleInstance)
                {
                    _logger.log(Logger.formatInfoRealTimeMessage(
                         "CryptoSoft is currently busy, waiting for it to be available...",
                         "DifferentialSave",
                         "",
                         DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                         SaveTaskState.WAITING
                     ));
                    setState(SaveTaskState.WAITING);
                    FileManager.WaitForInstance();
                }

                setState(SaveTaskState.RUNNING);

                // Call CryptoSoft DLL (Blocking operation)
                encryptionDuration = FileManager.CryptFile(sourceFile, diffFile, cryptoKey);
                if (encryptionDuration < 0)
                {
                    _logger.log(Logger.formatErrMessage($"Error encrypting file: {sourceFile}"));
                    return -1;
                }

                // Add full file size after encryption and update progress
                copiedBytes += fileInfo.Length;
                currentPercentage = totalBytes > 0 ? (int)((copiedBytes * 100) / totalBytes) : 100;
                progress?.Invoke(currentPercentage, fileInfo.Name);
            }
            else
            {
                // Standard copy using streams to allow pausing, stopping, and progress tracking mid-file
                using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                using (FileStream destinationStream = new FileStream(diffFile, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[81920]; // 80 KB buffer chunk
                    int bytesRead;

                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        // Check for Stop or Pause requests during the copy loop
                        token.ThrowIfCancellationRequested();

                        destinationStream.Write(buffer, 0, bytesRead);
                        copiedBytes += bytesRead;

                        // Update UI progress safely
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

        /// <summary>
        /// Executes the differential backup process. Compares the source with the full backup, 
        /// isolates modified or new files, and copies them to the destination.
        /// </summary>
        /// <param name="sourcePath">The path of the directory to be backed up.</param>
        /// <param name="destinationPath">The path where the differential backup will be stored.</param>
        /// <param name="priorityExt">A list of extensions dictating which files should be processed first.</param>
        /// <param name="token">A cancellation token to handle stop requests mid-process.</param>
        /// <param name="pauseEvent">A manual reset event to handle pause and resume functionalities.</param>
        /// <param name="progress">An action delegate used to push progress percentage updates and filenames to the UI thread.</param>
        /// <returns><c>true</c> if the save process successfully completed; otherwise, <c>false</c>.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the backup process is explicitly cancelled by the user.</exception>
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
                ConcurrentDictionary<string, object> dict_lock = SaveModel.getDictLock();
                object lock_var = dict_lock.GetOrAdd(destinationPath, _ => new object());

                setState(SaveTaskState.WAITING);
                lock (lock_var)
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

                    // Check if business software is running before starting the save process
                    if (isBusinessSoftwareRunning())
                    {
                        waitForBusinessSoftwareToClose();
                    }

                    // Get global encryption settings from Config Singleton
                    string cryptoKey = Config.Instance.getEncryptionKey();
                    List<string> cryptoExtensions = Config.Instance.getEncryptionExtensions();

                    _logger.log(Logger.formatLogMessage("Differential Save Started", sourcePath, destinationPath, 0, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                    // Initialize and clean destination directory
                    if (!Directory.Exists(destinationPath))
                    {
                        Directory.CreateDirectory(destinationPath);
                    }
                    else
                    {
                        // Clean the destination directory before starting
                        foreach (var file in Directory.EnumerateFiles(destinationPath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested(); // Check cancellation during cleanup
                            File.Delete(file);
                        }
                    }

                    // Pre-filter files using differential logic to calculate accurate totalBytes
                    var allFiles = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                    var filesToProcess = new List<string>();

                    foreach (var file in allFiles)
                    {
                        var relativePath = Path.GetRelativePath(sourcePath, file);
                        var fullFile = Path.Combine(_fullBackupPath, relativePath);

                        // Check if file is new or modified compared to the full backup
                        if (!File.Exists(fullFile) || File.GetLastWriteTime(file) > File.GetLastWriteTime(fullFile))
                        {
                            filesToProcess.Add(file);
                        }
                    }

                    // Calculate total bytes for the progress bar based ONLY on files that need copying
                    long totalBytes = filesToProcess.Sum(f => new FileInfo(f).Length);
                    long copiedBytes = 0; // Ready to be passed to processFile when updated

                    // Order filtered files based on priority extensions
                    var sortedFiles = filesToProcess.OrderBy(f =>
                    {
                        string ext = Path.GetExtension(f);
                        int index = priorityExt.FindIndex(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
                        return index >= 0 ? index : int.MaxValue;
                    }).ToArray();

                    // Main File Loop
                    foreach (var sourceFile in sortedFiles)
                    {
                        // Respect pause and cancellation requests
                        token.ThrowIfCancellationRequested();

                        if (!pauseEvent.IsSet) progress?.Invoke(0, "Paused");

                        pauseEvent.Wait(token);

                        // Process pending big files first if semaphore is available
                        if (_pendingFiles.Count > 0 && _bigFileSemaphore.CurrentCount > 0)
                        {
                            while (_pendingFiles.Count > 0)
                            {
                                token.ThrowIfCancellationRequested();

                                if (!pauseEvent.IsSet) progress?.Invoke(0, "Paused");

                                pauseEvent.Wait(token);

                                _bigFileSemaphore.Wait(token);
                                string pendingFile = _pendingFiles.Dequeue();
                                int local_encryptionDuration = processFile(pendingFile, sourcePath, destinationPath, _fullBackupPath, cryptoKey, cryptoExtensions, totalBytes, ref copiedBytes, progress, token, pauseEvent);
                                _bigFileSemaphore.Release();

                                if (local_encryptionDuration < 0) return false;
                            }
                        }

                        // Check file size for big file handling
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
                            // Process normal file
                            int encryptionDuration = processFile(sourceFile, sourcePath, destinationPath, _fullBackupPath, cryptoKey, cryptoExtensions, totalBytes, ref copiedBytes, progress, token, pauseEvent);
                            if (encryptionDuration < 0) return false;
                            continue;
                        }
                    }

                    // Final check to process any remaining pending files after the main loop
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

                    // Log completion and update state
                    _logger.log(Logger.formatCompleteSaveMessage("Differential Save Finished", sourcePath, destinationPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    setState(SaveTaskState.COMPLETED);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                // Handle explicit cancellation
                setState(SaveTaskState.STOPPED);
                throw;
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                _logger.log(Logger.formatErrMessage($"An error occurred during differential save: {ex.Message}"));
                setState(SaveTaskState.FAILED);
                return false;
            }
        }
    }
}