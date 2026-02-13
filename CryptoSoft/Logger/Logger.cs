using System;
using System.Runtime.CompilerServices;

namespace EasySave.Logger {
    // Interface that defines logging methods for different severity levels
    public interface ILogger {
        // General logging method
        public void Log(Log entry);

        public void SetLogFile(string filePath); // Method to set the log file path

        // Specific methods for each log level
        public void Info(Log entry);
        public void Debug(Log entry);
        public void Warning(Log entry);
        public void Error(Log entry);
        public void Critical(Log entry);
    }

    // Class implementing the logging functionality
    public class Logger : ILogger {
        // File path where the logs will be stored
        private string _filePath;

        // Constructor that allows specifying a custom log file path
        // Defaults to "logs.txt" if no path is provided
        public Logger(string filePath = "logs.txt") {
            _filePath = filePath;
        }

        public void SetLogFile(string filePath) {
            _filePath = filePath; // Update the log file path
        }

        // Core method to log an entry
        public void Log(Log entry) {
            string extension = Path.GetExtension(_filePath).ToLower();
            
            ILogFile logFile = extension switch {
                ".xml" => new LogFileXML(),
                ".json" => new LogFileJSON(),
                _ => throw new NotSupportedException($"Extension '{extension}' non supportée pour le fichier de log."),
            };

            if (entry.Datetime == new DateTime()) entry.Datetime = DateTime.Now;

            logFile.Save(entry, _filePath);
        }


        // Shortcut method to log an entry as "Information"
        public void Info(Log entry) => LogWithLevel(entry, LogLevel.Information);

        // Shortcut method to log an entry as "Debug"
        public void Debug(Log entry) => LogWithLevel(entry, LogLevel.Debug);

        // Shortcut method to log an entry as "Warning"
        public void Warning(Log entry) => LogWithLevel(entry, LogLevel.Warning);

        // Shortcut method to log an entry as "Error"
        public void Error(Log entry) => LogWithLevel(entry, LogLevel.Error);

        // Shortcut method to log an entry as "Critical"
        public void Critical(Log entry) => LogWithLevel(entry, LogLevel.Critical);

        // Private helper that sets the log level and timestamp, then calls Log()
        private void LogWithLevel(Log entry, LogLevel level) {
            entry.Level = level;              // Set the appropriate log level
            entry.Datetime = DateTime.Now;    // Update the timestamp to the current time
            Log(entry);                       // Call the main Log() method
        }
    }
}
