using CryptoSoft;
using EasyLog;
using ProjetEasySave.Utils;
using System.Diagnostics;
using System.IO;
using System.Collections.Concurrent;

namespace ProjetEasySave.Model
{
    /// <summary>
    /// Represents the complete backup strategy implementation of the <see cref="ISaveStrategy"/> interface.
    /// </summary>
    /// <remarks>
    /// This strategy copies all files from the source directory to the destination directory. 
    /// It handles large files using a semaphore, encrypts files based on user configuration, 
    /// and pauses operations if restricted business software is running.
    /// </remarks>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="CompleteSave"/> class.
        /// </summary>
        /// <param name="saveTask">The main task object associated with this backup operation.</param>
        /// <param name="bigFileSemaphore">The semaphore used to limit concurrent transfers of large files across the application.</param>
        public CompleteSave(SaveTask saveTask, SemaphoreSlim bigFileSemaphore)
        {
            _saveTask = saveTask;
            _bigFileSemaphore = bigFileSemaphore;
            _pendingFiles = new Queue<string>();
        }

        /// <summary>
        /// Checks whether the configured business software is currently running on the system.
        /// </summary>
        /// <returns><c>true</c> if the business software is running; otherwise, <c>false</c>.</returns>
        public bool isBusinessSoftwareRunning()
        {
            // Name of the process to look for
            string businessSoftwareName = config.getBusinessSoftwareName();

            // Fetch all processes matching the specified name
            Process[] processes = Process.GetProcessesByName(businessSoftwareName);

            // Return true if at least one instance is actively running
            return processes.Length > 0;
        }

        /// <summary>
        /// Suspends the backup thread and waits until all instances of the conflicting business software are closed.
        /// </summary>
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

        /// <summary>
        /// Updates the execution state of the backup task and notifies listeners.
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
        /// Processes a single file by copying it to the destination, applying encryption if required.
        /// </summary>
        /// <param name="file">The full path of the source file.</param>
        /// <param name="sourcePath">The root directory path of the backup source.</param>
        /// <param name="destinationPath">The root directory path of the backup destination.</param>
        /// <param name="cryptoKey">The key used for file encryption.</param>
        /// <param name="cryptoExtensions">A list of file extensions that require encryption.</param>
        /// <param name="totalBytes">The total size in bytes of the entire backup job.</param>
        /// <param name="copiedBytes">The total number of bytes successfully copied so far (passed by reference).</param>
        /// <param name="token">A cancellation token to observe for stop requests.</param>
        /// <param name="pauseEvent">A reset event used to pause and resume the copying process.</param>
        /// <param name="progress">A delegate used to report the progress percentage and the current file name to the UI.</param>
        /// <returns>The duration of the encryption process in milliseconds, <c>0</c> if no encryption occurred, or <c>-1</c> if an error happened.</returns>
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
            Action<int, string> progress
        )
        {
            var relative = Path.GetRelativePath(sourcePath, file);
            var targetFile = Path.Combine(destinationPath, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            FileInfo fileInfo = new FileInfo(file);
            string extension = Path.GetExtension(file);

            if (isBusinessSoftwareRunning())
            {
                waitForBusinessSoftwareToClose();
            }

            // Information: 0 (no encryption), >0 (time in ms), <0 (error code)
            double encryptionDuration = 0;
            DateTime startTime = DateTime.Now;

            bool shouldEncrypt = !string.IsNullOrEmpty(cryptoKey)
                                 && cryptoExtensions != null
                                 && cryptoExtensions.Any(e => e.Equals(extension, StringComparison.OrdinalIgnoreCase));

            // Update UI with the current file name before starting
            int currentPercentage = totalBytes > 0 ? (int)((copiedBytes * 100) / totalBytes) : 0;
            progress?.Invoke(currentPercentage, fileInfo.Name);

            if (shouldEncrypt)
            {
                // Check Cryptosoft is mono instance and wait if necessary
                if (!FileManager.IsSingleInstance)
                {
                   _logger.log(Logger.formatInfoRealTimeMessage(
                        "CryptoSoft is currently busy, waiting for it to be available...",
                        "CompleteSave",
                        "",
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        SaveTaskState.WAITING
                    ));
                    setState(SaveTaskState.WAITING);
                    FileManager.WaitForInstance();
                }

                setState(SaveTaskState.RUNNING);

                // Call CryptoSoft DLL (Blocking operation)
                encryptionDuration = FileManager.CryptFile(file, targetFile, cryptoKey);
                if (encryptionDuration < 0)
                {
                    _logger.log(Logger.formatErrMessage($"Error encrypting file: {file}"));
                    return -1; // Indicate error
                }

                // Add full file size after encryption and update progress
                copiedBytes += fileInfo.Length;
                currentPercentage = totalBytes > 0 ? (int)((copiedBytes * 100) / totalBytes) : 100;
                progress?.Invoke(currentPercentage, fileInfo.Name);
            }
            else
            {
                // Standard copy using streams to allow pausing and stopping mid-file
                using (FileStream sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                using (FileStream destinationStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write))
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
                shouldEncrypt ? "Copying File (Encrypted)" : "Copying File",
                file,
                targetFile,
                (int)fileInfo.Length,
                encryptionDuration,
                startTime.ToString("yyyy-MM-dd HH:mm:ss")
            ));

            return (int)encryptionDuration;
        }

        /// <summary>
        /// Executes the complete backup process, reading files from the source directory, formatting the destination, and copying all contents.
        /// </summary>
        /// <param name="sourcePath">The path of the directory to be backed up.</param>
        /// <param name="destinationPath">The path where the backup will be stored.</param>
        /// <param name="priorityExt">A list of extensions dictating which files should be processed first.</param>
        /// <param name="token">A cancellation token to handle stop requests mid-process.</param>
        /// <param name="pauseEvent">A manual reset event to handle pause and resume functionalities.</param>
        /// <param name="progress">An action delegate used to push progress updates to the UI thread.</param>
        /// <returns><c>true</c> if the save process successfully completed; otherwise, <c>false</c>.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the backup process is explicitly cancelled by the user.</exception>
        public bool doSave(
            string sourcePath,
            string destinationPath,
            List<string> priorityExt,
            CancellationToken token,
            ManualResetEventSlim pauseEvent,
            Action<int, string> progress
        )
        {
            try
            {
                ConcurrentDictionary<string, object> dict_lock = SaveModel.getDictLock();
                object lock_var = dict_lock.GetOrAdd(destinationPath, _ => new object());

                setState(SaveTaskState.WAITING);

                _pendingFiles.Clear();
                lock (lock_var)
                {
                    setState(SaveTaskState.RUNNING);
                    // Validate paths
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

                    // Check if business software is running before starting the save process
                    if (isBusinessSoftwareRunning())
                    {
                        waitForBusinessSoftwareToClose();
                    }

                    // Initial log
                    _logger.log(Logger.formatLogMessage("Complete Save Started", sourcePath, destinationPath, 0, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                    // Fetch keys and extensions from the Config singleton
                    string cryptoKey = Config.Instance.getEncryptionKey();
                    List<string> cryptoExtensions = Config.Instance.getEncryptionExtensions();

                    // Initialize and clean destination directory
                    if (!Directory.Exists(destinationPath))
                    {
                        // Create directory structure
                        foreach (var dir in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                        {
                            var relative = Path.GetRelativePath(sourcePath, dir);
                            var targetDir = Path.Combine(destinationPath, relative);
                            Directory.CreateDirectory(targetDir);
                        }
                    }
                    else
                    {
                        // Clean the destination directory before starting
                        foreach (var file in Directory.EnumerateFiles(destinationPath, "*", SearchOption.AllDirectories))
                        {
                            token.ThrowIfCancellationRequested(); // Check cancellation even during cleanup
                            File.Delete(file);
                        }
                    }

                    // Fetch all files and calculate total bytes for progress reporting
                    string[] files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                    long totalBytes = files.Sum(f => new FileInfo(f).Length);
                    long copiedBytes = 0;

                    // Order files based on priority extensions
                    files = files.OrderBy(f =>
                    {
                        string ext = Path.GetExtension(f);
                        int index = priorityExt.FindIndex(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
                        return index >= 0 ? index : int.MaxValue;
                    }).ToArray();

                    // Main File Loop
                    foreach (var file in files)
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

                        // Check if current file is big and handle semaphore logic
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
                            // Process normal file
                            int encryptionDuration = processFile(file, sourcePath, destinationPath, cryptoKey, cryptoExtensions, totalBytes, ref copiedBytes, token, pauseEvent, progress);
                            if (encryptionDuration < 0) return false;
                            continue;
                        }
                    }

                    // Final check to process any remaining pending files
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

                    // Log completion and update state
                    _logger.log(Logger.formatCompleteSaveMessage("Complete Save Finished", sourcePath, destinationPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
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
                _logger.log(Logger.formatErrMessage($"Error during complete save: {ex.Message}"));
                setState(SaveTaskState.FAILED);
                return false;
            }
        }
    }
}