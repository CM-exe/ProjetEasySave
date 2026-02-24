using CryptoSoft;
using EasyLog;
using ProjetEasySave.Utils;
using ProjetEasySave.ViewModel;
using System.Collections.Concurrent;
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

        private int proccesFile(string file, string sourcePath, string destinationPath, string cryptoKey, List<string> cryptoExtensions)
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
                                 // Utilise LINQ pour vérifier si l'extension existe, en ignorant la casse
                                 && cryptoExtensions.Any(e =>
                                     e.Equals(extension, StringComparison.OrdinalIgnoreCase));

            if (shouldEncrypt)
            {
                // Call CryptoSoft DLL
                // Returns time in ms or -1 on error
                encryptionDuration = FileManager.CryptFile(file, targetFile, cryptoKey);
                if (encryptionDuration < 0)
                {
                    _logger.log(Logger.formatErrMessage($"Error encrypting file: {file}"));
                    return -1; // Indicate error
                }
            }
            else
            {
                // Standard copy (encryptionDuration remains 0)
                File.Copy(file, targetFile, true);
            }

            // Passing encryptionDuration: 0 if standard copy, result of DLL if encrypted
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
        public bool doSave(string sourcePath, string destinationPath, List<string> priorityExt)
        {
            try
            {
                ConcurrentDictionary<string, object> dict_lock = SaveModel.getDictLock();
                object lock_var = dict_lock.GetOrAdd(destinationPath, _ => new object());
                lock (lock_var) {
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

                    // Check a first time if business software is running before starting the save process
                    if (isBusinessSoftwareRunning())
                    {
                        waitForBusinessSoftwareToClose();
                    }

                    // Initial Log
                    _logger.log(Logger.formatLogMessage("Complete Save Started", sourcePath, destinationPath, 0, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                    // Fetching keys and extensions from your Config singleton
                    string cryptoKey = Config.Instance.getEncryptionKey();
                    List<string> cryptoExtensions = Config.Instance.getEncryptionExtensions();


                    // Initialize destination directory
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
                            File.Delete(file);
                        }
                    }

                    // Main File Loop
                    string[] files = Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories);
                    // Order files based on priority extensions (files with priority extensions first)
                    files = files.OrderBy(f =>
                    {
                        string ext = Path.GetExtension(f);
                        int index = priorityExt.FindIndex(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
                        return index >= 0 ? index : int.MaxValue;
                    }).ToArray();
                    foreach (var file in files)
                    {
                        if (isBusinessSoftwareRunning())
                        {
                            waitForBusinessSoftwareToClose();
                        }

                        // If the queue is not empty and the semaphore is available, process the pending files first
                        if (_pendingFiles.Count > 0 && _bigFileSemaphore.CurrentCount > 0)
                        {
                            while (_pendingFiles.Count > 0)
                            {
                                // Take the semaphore to process the pending file
                                _bigFileSemaphore.Wait();
                                // Process the pending file
                                string pendingFile = _pendingFiles.Dequeue();
                                int local_encryptionDuration = proccesFile(pendingFile, sourcePath, destinationPath, cryptoKey, cryptoExtensions);
                                // Release the semaphore after processing
                                _bigFileSemaphore.Release();
                                if (local_encryptionDuration < 0)
                                {
                                    return false; // Abort if the encryption/copy process failed
                                }
                            }
                        }

                        // Check if the current file to process is too big (greater than the threshold defined in config) and if the semaphore is not available (another big file is being processed)
                        FileInfo fileInfo = new FileInfo(file);
                        if (fileInfo.Length > (config.getBiggestSize() * 1000) && _bigFileSemaphore.CurrentCount == 0) // * 1000 to convert from o to Ko
                        {
                            // Add it to the pending queue and continue with the next file
                            _pendingFiles.Enqueue(file);
                            continue;
                        }
                        else if (fileInfo.Length > (config.getBiggestSize() * 1000) && _bigFileSemaphore.CurrentCount > 0)
                        {
                            // If the file is big but the semaphore is available, take the semaphore and process it immediately
                            _bigFileSemaphore.Wait();
                            int local_encryptionDuration = proccesFile(file, sourcePath, destinationPath, cryptoKey, cryptoExtensions);
                            _bigFileSemaphore.Release();
                            if (local_encryptionDuration < 0)
                            {
                                return false; // Abort if the encryption/copy process failed
                            }
                            continue;
                        }
                        else
                        {
                            int encryptionDuration = proccesFile(file, sourcePath, destinationPath, cryptoKey, cryptoExtensions);

                            // Abort if the encryption/copy process failed (duration < 0)
                            if (encryptionDuration < 0) return false;
                            continue;
                        }
                    }

                    // Final check to process any remaining pending files after the main loop
                    while (_pendingFiles.Count > 0)
                    {
                        _bigFileSemaphore.Wait();
                        string pendingFile = _pendingFiles.Dequeue();
                        int local_encryptionDuration = proccesFile(pendingFile, sourcePath, destinationPath, cryptoKey, cryptoExtensions);
                        _bigFileSemaphore.Release();
                        if (local_encryptionDuration < 0)
                        {
                            return false; // Abort if the encryption/copy process failed
                        }
                    }

                    // "Save completed" Log
                    _logger.log(Logger.formatCompleteSaveMessage(
                        "Complete Save Finished",
                        sourcePath,
                        destinationPath,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    ));
                    setState(SaveTaskState.COMPLETED);
                }

                return true;
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