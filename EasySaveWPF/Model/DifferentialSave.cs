using ProjetEasySave.Utils;
using System.IO;
using CryptoSoft;

namespace ProjetEasySave.Model
{
    public class DifferentialSave : ISaveStrategy
    {
        /*
         * Differential Save implementation of the ISaveStrategy interface.
         * This strategy only saves files that have changed since the last full backup.
         */

        // Attributes
        private Logger _logger = Logger.getInstance();
        private string _fullBackupPath; // Reference path of the full backup

        // Constructor
        public DifferentialSave(string fullBackupPath)
        {
            _fullBackupPath = fullBackupPath;
        }

        // Methods
        public string getCompleteSavePath()
        {
            return _fullBackupPath;
        }

        // doSave method implementation for Differential Save
        public bool doSave(string sourcePath, string destinationPath, Func<bool> businessSoftwareChecker = null)
        {
            try
            {
                // Validate paths
                if (string.IsNullOrWhiteSpace(sourcePath) ||
                    string.IsNullOrWhiteSpace(destinationPath) ||
                    string.IsNullOrWhiteSpace(_fullBackupPath))
                {
                    _logger.log(Logger.formatErrMessage("Source, destination, or full backup path is invalid."));
                    return false;
                }

                if (!Directory.Exists(sourcePath))
                {
                    _logger.log(Logger.formatErrMessage($"Source path '{sourcePath}' does not exist."));
                    return false;
                }

                if (!Directory.Exists(_fullBackupPath))
                {
                    _logger.log(Logger.formatErrMessage($"Full backup path '{_fullBackupPath}' does not exist."));
                    return false;
                }

                // Get global encryption settings from Config Singleton
                string cryptoKey = Config.Instance.EncryptionKey;
                List<string> cryptoExtensions = Config.Instance.EncryptionExtensions;

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

                foreach (var sourceFile in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
                {
                    if (businessSoftwareChecker != null && businessSoftwareChecker())
                    {
                        _logger.log(Logger.formatErrMessage("Backup suspended: Business software detected."));
                        return false;
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

                return true;
            }
            catch (Exception ex)
            {
                _logger.log(Logger.formatErrMessage($"An error occurred during differential save: {ex.Message}"));
                return false;
            }
        }
    }
}