using ProjetEasySave.Utils;

namespace ProjetEasySave.Model
{
    public class DifferentialSave : ISaveStrategy
    {
        /*
         * Differential Save implementation of the ISaveStrategy interface.
         */

        // Attributes
        Logger _logger = Logger.getInstance();

        // Constructor
        public DifferentialSave()
        {
        }

        // doSave method implementation for Differential Save
        public bool doSave(string sourcePath, string destinationPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destinationPath))
                {
                    _logger.log(Logger.formatErrMessage("Source or destination path is invalid."));
                    return false;
                }

                if (!Directory.Exists(sourcePath))
                {
                    _logger.log(Logger.formatErrMessage($"Source path '{sourcePath}' does not exist."));
                    return false;
                }

                Directory.CreateDirectory(destinationPath);

                foreach (var sourceFile in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(sourcePath, sourceFile);
                    var destinationFile = Path.Combine(destinationPath, relativePath);

                    if (File.Exists(destinationFile))
                    {
                        continue;
                    }

                    var destinationDir = Path.GetDirectoryName(destinationFile);
                    if (!string.IsNullOrEmpty(destinationDir))
                    {
                        _logger.log(Logger.formatLogMessage("Creating directory", sourcePath, destinationDir, 0, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                        Directory.CreateDirectory(destinationDir);
                    }

                    _logger.log(Logger.formatLogMessage("Copying file", sourceFile, destinationFile, 0, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    File.Copy(sourceFile, destinationFile, overwrite: false);
                }

                _logger.log(Logger.formatLogMessage("Differential Save", sourcePath, destinationPath, 0, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
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