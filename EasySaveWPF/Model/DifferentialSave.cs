using ProjetEasySave.Utils;
using System.IO;
using EasyLog;

namespace ProjetEasySave.Model
{
    public class DifferentialSave : ISaveStrategy
    {
        /*
         * Differential Save implementation of the ISaveStrategy interface.
         */

        // Attributes
         private Logger _logger = Logger.getInstance(Config.Instance);

         private string _fullBackupPath;  // Chemin de la sauvegarde complète servant de référence

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
                if (businessSoftwareChecker != null && businessSoftwareChecker())
                {
                    // Log the specific error
                    Logger.getInstance(Config.Instance).log(Logger.formatErrMessage("Backup suspended: Business software detected."));

                    // Stop the backup immediately (after the previous file is done)
                    return false;
                }

                if (string.IsNullOrWhiteSpace(sourcePath) ||
                    string.IsNullOrWhiteSpace(destinationPath) ||
                    string.IsNullOrWhiteSpace(_fullBackupPath)
                    )
                {
                    _logger.log(Logger.formatErrMessage("Source or full backup is invalid."));
                    return false;
                }

                if (!Directory.Exists(sourcePath))
                {
                    _logger.log(Logger.formatErrMessage($"Source path '{sourcePath}' does not exist."));
                    return false;
                }

                if (!Directory.Exists(_fullBackupPath))
                {
                    _logger.log(Logger.formatErrMessage($"Full backup '{_fullBackupPath}"));
                    return false;
                }


                _logger.log(Logger.formatLogMessage(
                    "Differntial Save Started",
                    sourcePath,
                    destinationPath,
                    0, // Default size (0 at the start)
                    0, // Default transfer time (0 at the start)
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    ));


                // Create the destination directory if it doesn't exist
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                }
                else
                {
                    // Clear the destination directory before copying new files
                    foreach (var file in Directory.EnumerateFiles(destinationPath, "*", SearchOption.AllDirectories))
                    {
                        File.Delete(file);
                    }
                }

                foreach (var sourceFile in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                    var fullFile = Path.Combine(_fullBackupPath, relativePath);
                    var diffFile = Path.Combine(destinationPath, relativePath);


                    bool shouldCopy = false;

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

                    if (!shouldCopy)
                        continue;

                    _logger.log(Logger.formatLogMessage(
                        "Copying File (Differential)",
                        sourceFile,
                        diffFile,
                        0,
                        (int)new FileInfo(sourceFile).Length,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        ));

                    File.Copy(sourceFile, diffFile, true);

                }

                return true;

            }

            catch
            {
                _logger.log(Logger.formatErrMessage("An error occurred during the differential save process."));
                return false;
            }
        }
    }
}