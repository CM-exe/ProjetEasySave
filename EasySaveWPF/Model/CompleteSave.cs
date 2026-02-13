using ProjetEasySave.Utils;
using System.IO;
using CryptoSoft;

namespace ProjetEasySave.Model
{
    public class CompleteSave : ISaveStrategy
    {
        /*
         * Complete Save implementation of the ISaveStrategy interface.
         */

        // Attributes
        private Logger _logger = Logger.getInstance();

        // Constructor
        public CompleteSave() { }

        public bool doSave(string sourcePath, string destinationPath, Func<bool> businessSoftwareChecker = null)
        {
            try
            {
                // Validate paths
                if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath))
                {
                    _logger.log(Logger.formatErrMessage("Source or destination path is invalid."));
                    return false;
                }

                if (!Directory.Exists(sourcePath))
                {
                    _logger.log(Logger.formatErrMessage($"Source path does not exist: {sourcePath}"));
                    return false;
                }

                // Initial Log
                _logger.log(Logger.formatLogMessage("Complete Save Started", sourcePath, destinationPath, 0, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

                Directory.CreateDirectory(destinationPath);

                // Fetching keys and extensions from your Config singleton
                string cryptoKey = Config.Instance.EncryptionKey;
                List<string> cryptoExtensions = Config.Instance.EncryptionExtensions;

                // Create directory structure
                foreach (var dir in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(sourcePath, dir);
                    var targetDir = Path.Combine(destinationPath, relative);
                    Directory.CreateDirectory(targetDir);
                }

                // Main File Loop
                foreach (var file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
                {
                    if (businessSoftwareChecker != null && businessSoftwareChecker())
                    {
                        _logger.log(Logger.formatErrMessage("Backup suspended: Business software detected."));
                        return false;
                    }

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

                    // Abort if the encryption/copy process failed (duration < 0)
                    if (encryptionDuration < 0) return false;
                }

                // "Save completed" Log
                _logger.log(Logger.formatCompleteSaveMessage(
                    "Complete Save Finished",
                    sourcePath,
                    destinationPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                ));

                return true;
            }
            catch (Exception ex)
            {
                _logger.log(Logger.formatErrMessage($"Error during complete save: {ex.Message}"));
                return false;
            }
        }
    }
}