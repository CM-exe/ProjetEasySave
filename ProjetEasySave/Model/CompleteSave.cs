using ProjetEasySave.Utils;

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
        public CompleteSave()
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
                    _logger.log(Logger.formatErrMessage($"Source path does not exist: {sourcePath}"));
                    return false;
                }

                _logger.log(Logger.formatLogMessage("Complete Save Started", sourcePath, destinationPath, 0, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                Directory.CreateDirectory(destinationPath);

                foreach (var dir in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(sourcePath, dir);
                    var targetDir = Path.Combine(destinationPath, relative);
                    _logger.log(Logger.formatLogMessage("Creating Directory", dir, targetDir, 0, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    Directory.CreateDirectory(targetDir);
                }

                foreach (var file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
                {
                    var relative = Path.GetRelativePath(sourcePath, file);
                    var targetFile = Path.Combine(destinationPath, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                    _logger.log(Logger.formatLogMessage("Copying File", file, targetFile, (int)new FileInfo(file).Length, 0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
                    File.Copy(file, targetFile, true);
                }

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