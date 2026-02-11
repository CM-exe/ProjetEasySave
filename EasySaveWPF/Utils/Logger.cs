using System;
using System.Collections.Generic;
using System.Text;

namespace ProjetEasySave.Utils
{
    public class Logger
    {
        /*
         * Logger class implementing the Singleton design pattern.
         * This class is responsible for logging application events to a specified log file in JSON format.
         * Usage exemple:
         * Logger logger = Logger.getInstance()
         */

        // Attributes
        private static Logger singletonInstance; // Has to be static
        // Load config
        Config config = Config.Instance;
        private string logDirectoryPath;
        private string logRealTimeFile;

        // Constructors
        private Logger()
        {
            // Default log directory path
            logDirectoryPath = config.getLogDirectoryPath();
            // Create the log directory if it doesn't exist
            if (!System.IO.Directory.Exists(logDirectoryPath))
            {
                System.IO.Directory.CreateDirectory(logDirectoryPath);
            }
            // Default log file for real-time logging
            logRealTimeFile = System.IO.Path.Combine(logDirectoryPath, "real_time_log.json");
        }

        // Methods
        public static Logger getInstance() // Doit être static
        {
            if (singletonInstance == null)
            {
                singletonInstance = new Logger();
            }
            return singletonInstance;

        }

        // Logging methods
        public bool log(Dictionary<string, string> message)
        {
            // Check if logFile is set
            if (logDirectoryPath == null)
            {
                return false;
            }

            // Define log file path
            string todaysDate = DateTime.Today.ToString("yyyy-MM-dd");
            string logFilePath = System.IO.Path.Combine(logDirectoryPath, todaysDate + "_log.json");

            // Build the message at the good format
            StringBuilder jsonMessage = new StringBuilder();
            jsonMessage.Append("{");
            foreach (var kvp in message)
            {
                jsonMessage.AppendFormat("\"{0}\": \"{1}\",", kvp.Key, kvp.Value);
            }
            if (message.Count > 0)
            {
                jsonMessage.Length--;
            }
            jsonMessage.Append("}");
            string jsonString = jsonMessage.ToString();

            // If file not existing, create it and add the message
            if (!System.IO.File.Exists(logFilePath) || new System.IO.FileInfo(logFilePath).Length == 0)
            {
                System.IO.File.WriteAllText(logFilePath, "[" + jsonString + "]");
                return true;
            }

            // Else, add the message
            string existing = System.IO.File.ReadAllText(logFilePath).TrimEnd();
            if (existing.EndsWith("]"))
            {
                existing = existing.Substring(0, existing.Length - 1);
                if (existing.EndsWith("["))
                {
                    existing += jsonString + "]";
                }
                else
                {
                    existing += "," + jsonString + "]";
                }
                System.IO.File.WriteAllText(logFilePath, existing);
                return true;
            }

            System.IO.File.WriteAllText(logFilePath, "[" + jsonString + "]");
            return true;
        }

        public bool logRealTime(Dictionary<string, string> message)
        {
            // Check if logFile is set
            if (logDirectoryPath == null)
            {
                return false;
            }
            // Build the message at the good format
            StringBuilder jsonMessage = new StringBuilder();
            jsonMessage.Append("{");
            foreach (var kvp in message)
            {
                jsonMessage.AppendFormat("\"{0}\": \"{1}\",", kvp.Key, kvp.Value);
            }
            if (message.Count > 0)
            {
                jsonMessage.Length--;
            }
            jsonMessage.Append("}");
            string jsonString = jsonMessage.ToString();
            // Write the message to the real-time log file
            System.IO.File.WriteAllText(logRealTimeFile, jsonString);
            return true;
        }

        public static Dictionary<string, string> formatInfoRealTimeMessage(string name, string source, string target, string time, SaveTaskState saveTaskState)
        {
            // Format the log message as a dictionary
            Dictionary<string, string> logMessage = new Dictionary<string, string>
            {
                { "name", name },
                { "sourceFile", source },
                { "targetFile", target },
                { "time", time },
                { "saveTaskState", saveTaskState.ToString() }
            };
            return logMessage;
        }

        public static Dictionary<string, string> formatLogMessage(string name, string source, string target, int size, double transferTime, string time)
        {
            // Format the log message as a dictionary
            Dictionary<string, string> logMessage = new Dictionary<string, string>
            {
                { "name", name },
                { "sourceFile", source },
                { "targetFile", target },
                { "size", size.ToString() },
                { "transferTime", transferTime.ToString() },
                { "time", time }
            };

            return logMessage;
        }

        public static Dictionary<string, string> formatErrMessage(string errorMessage)
        {
            // Format the log message as a dictionary
            Dictionary<string, string> logMessage = new Dictionary<string, string>
            {
                { "error", errorMessage },
                { "time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            };
            return logMessage;
        }
    }
}