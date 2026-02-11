using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace ProjetEasySave.Utils
{
    // Enumeration to define the supported log formats
    public enum LogFormat { Json, Xml }

    public class Logger
    {
        // Attributes
        private static Logger singletonInstance;
        private string logDirectoryPath;
        private string logRealTimeFile;
        
        // Property to define the current format (adjustable by the user)
        public LogFormat CurrentFormat { get; set; } = LogFormat.Json;

        // Private constructor for Singleton pattern
        private Logger()
        {
            // Default log directory path
            logDirectoryPath = @"C:\EasyProject\Logs\"; 
            if (!Directory.Exists(logDirectoryPath))
            {
                Directory.CreateDirectory(logDirectoryPath);
            }

            // Initialize the base path for real-time logging
            logRealTimeFile = Path.Combine(logDirectoryPath, "real_time_log");
        }

        // Method to get the unique instance of the Logger
        public static Logger getInstance()
        {
            if (singletonInstance == null)
                singletonInstance = new Logger();
            return singletonInstance;
        }

        // --- Historical Logging Method (Append mode) ---
        public bool log(Dictionary<string, string> message)
        {
            if (logDirectoryPath == null)
            {
                Console.WriteLine("Log directory path is not set.");
                return false;
            }

            string extension = CurrentFormat == LogFormat.Json ? ".json" : ".xml";
            string todaysDate = DateTime.Today.ToString("yyyy-MM-dd");
            string logFilePath = Path.Combine(logDirectoryPath, todaysDate + "_log" + extension);

            // Format content based on the selected format
            string formattedContent = CurrentFormat == LogFormat.Json 
                ? FormatToJson(message) 
                : FormatToXml(message);

            return WriteToFile(logFilePath, formattedContent, CurrentFormat);
        }

        // --- Real-Time Logging Method (Overwrite mode) ---
        public bool logRealTime(Dictionary<string, string> message)
        {
            try
            {
                string fullPath = logRealTimeFile + (CurrentFormat == LogFormat.Json ? ".json" : ".xml");

                string content = CurrentFormat == LogFormat.Json 
                    ? FormatToJson(message) 
                    : FormatToXml(message);

                // Overwrites the file to keep only the most recent state
                File.WriteAllText(fullPath, content);
                return true;
            }
            catch { return false; }
        }

        // --- Private Formatting Methods ---

        private string FormatToJson(Dictionary<string, string> message)
        {
            var entries = message.Select(kvp => $"\"{kvp.Key}\": \"{kvp.Value}\"");
            return "{" + string.Join(",", entries) + "}";
        }

        private string FormatToXml(Dictionary<string, string> message)
        {
            StringBuilder xml = new StringBuilder();
            xml.Append("<logEntry>");
            foreach (var kvp in message)
            {
                xml.AppendFormat("<{0}>{1}</{0}>", kvp.Key, kvp.Value);
            }
            xml.Append("</logEntry>");
            return xml.ToString();
        }

        // Logic to handle JSON arrays and XML root tags during file writing
        private bool WriteToFile(string path, string content, LogFormat format)
        {
            try
            {
                // If file doesn't exist, initialize it with correct structure
                if (!File.Exists(path) || new FileInfo(path).Length == 0)
                {
                    string initial = format == LogFormat.Json 
                        ? "[" + content + "]" 
                        : "<root>\n" + content + "\n</root>";
                    File.WriteAllText(path, initial);
                }
                else
                {
                    if (format == LogFormat.Json)
                    {
                        string existing = File.ReadAllText(path).Trim();
                        if (existing.EndsWith("]"))
                        {
                            existing = existing.Substring(0, existing.Length - 1);
                            string separator = (existing.EndsWith("[")) ? "" : ",";
                            File.WriteAllText(path, existing + separator + content + "]");
                        }
                    }
                    else // XML Handling
                    {
                        string existing = File.ReadAllText(path).Trim();
                        if (existing.EndsWith("</root>"))
                        {
                            existing = existing.Substring(0, existing.Length - 7);
                            File.WriteAllText(path, existing + content + "\n</root>");
                        }
                    }
                }
                return true;
            }
            catch { return false; }
        }

        // --- Data Formatting Methods (Static) ---

        public static Dictionary<string, string> formatInfoRealTimeMessage(string name, string source, string target, string time, SaveTaskState saveTaskState)
        {
            // Format the log message as a dictionary
            return new Dictionary<string, string>
            {
                { "name", name },
                { "sourceFile", source },
                { "targetFile", target },
                { "time", time },
                { "saveTaskState", saveTaskState.ToString() }
            };
        }

        public static Dictionary<string, string> formatLogMessage(string name, string source, string target, int size, double transferTime, string time)
        {
            // Format the log message as a dictionary
            return new Dictionary<string, string>
            {
                { "name", name },
                { "sourceFile", source },
                { "targetFile", target },
                { "size", size.ToString() },
                { "transferTime", transferTime.ToString() },
                { "time", time }
            };
        }

        public static Dictionary<string, string> formatErrMessage(string errorMessage)
        {
            // Format the log message as a dictionary
            return new Dictionary<string, string>
            {
                { "error", errorMessage },
                { "time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            };
        }
    }
}