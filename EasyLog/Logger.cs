using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;

namespace EasyLog
{
    // Enumeration to define the supported log formats
    public enum LogFormat { Json, Xml }

    public class Logger
    {
        // Attributes
        private static Logger singletonInstance;
        private string logDirectoryPath;
        private string logRealTimeFile;
        private static IConfig _config; // Load config
        private static Socket loggingServerSocket; // Socket for logging server connection



        // Property to define the current format (adjustable by the user)
        private LogFormat currentFormat = LogFormat.Json;

        // Private constructor for Singleton pattern
        private Logger(IConfig config)
        {
            _config = config;
            // Default log directory path
            logDirectoryPath = _config.getLogDirectoryPath();
            if (!Directory.Exists(logDirectoryPath))
            {
                Directory.CreateDirectory(logDirectoryPath);
            }

            currentFormat = _config.getLogsFormat().ToLower() == "xml" ? LogFormat.Xml : LogFormat.Json;

            // Initialize the base path for real-time logging
            logRealTimeFile = _config.getLogRealTimeFile();

            // Establish a socket connection to the logging server for logging server
            try
            {
                loggingServerSocket = connectToServer();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to connect to logging server: " + ex.Message);
                loggingServerSocket = null; // Set to null if connection fails
            }
        }

        // Method to get the unique instance of the Logger
        public static Logger getInstance(IConfig config)
        {
            if (singletonInstance == null)
                singletonInstance = new Logger(config);
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

            string extension = currentFormat == LogFormat.Json ? ".json" : ".xml";
            string todaysDate = DateTime.Today.ToString("yyyy-MM-dd");
            string logFilePath = Path.Combine(logDirectoryPath, todaysDate + "_log" + extension);

            bool wellExecuted = true;

            // Format content based on the selected format
            string formattedContent = currentFormat == LogFormat.Json
                ? FormatToJson(message)
                : FormatToXml(message);

            if (loggingServerSocket != null && _config.getBoolLogsOnServer()) {
                try
                {
                    sendMessageToServer(loggingServerSocket, formattedContent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to send log to server: " + ex.Message);
                    // If sending fails, we can choose to fallback to file logging or just return false
                    // For this implementation, we'll fallback to file logging
                    wellExecuted = false; 
                }
            }

            if (_config.getBoolLogsOnLocal())
            {
                try
                {
                    WriteToFile(logFilePath, formattedContent, currentFormat);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to write log to file: " + ex.Message);
                    wellExecuted = false;
                }
            }
            return wellExecuted;
        }

        // --- Socket connection to the logging server method ---
        private static Socket connectToServer()
        {
            // Create a new socket for the client
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Connect to the server using the config values for IP and port
            socket.Connect(new IPEndPoint(IPAddress.Parse(_config.getServerIp()),_config.getServerPort()));
            return socket;
        }

        private static void sendMessageToServer(Socket socket, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            socket.Send(data);
        }

        // --- Real-Time Logging Method (Overwrite mode) ---
        public bool logRealTime(Dictionary<string, string> message)
        {
            try
            {
                string fullPath = logRealTimeFile + (currentFormat == LogFormat.Json ? ".json" : ".xml");

                string content = currentFormat == LogFormat.Json
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
            // Add the first element to be the Environement.UserName to identify the user who performed the save
            if (message == null)
            {
                message = new Dictionary<string, string>();
            }
            message["user"] = Environment.UserName;
            // We create a collection of formatted strings for each key-value pair
            var entries = message.Select(kvp =>
            {
                string escapedValue = kvp.Value
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"");

                return $"\"{kvp.Key}\": \"{escapedValue}\"";
            });

            return "{" + string.Join(",", entries) + "}";
        }

        private string FormatToXml(Dictionary<string, string> message)
        {
            StringBuilder xml = new StringBuilder();
            xml.Append("<logEntry>");
            // Add the first element to be the Environement.UserName to identify the user who performed the save
            xml.AppendFormat("<user>{0}</user>", Environment.UserName);
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

        public static Dictionary<string, string> formatInfoRealTimeMessage(string name, string source, string target, string time, dynamic saveTaskState)
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

        public static Dictionary<string, string> formatCompleteSaveMessage(string name, string source, string target, string time)
        {
            return new Dictionary<string, string>
            {
                { "name", name },
                { "sourceFile", source },
                { "targetFile", target },
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

        // Getters and Setters
        public LogFormat getCurrentFormat()
        {
            return currentFormat;
        }

        public void setCurrentFormat(LogFormat format)
        {
            currentFormat = format;
            _config.setLogsFormat(format == LogFormat.Json ? "json" : "xml"); // Save the new format in the config file
        }

        // Using string for easier interaction with the UI
        public string getLogsFormat()
        {
            return currentFormat.ToString();
        }

        public void setLogsFormat(string format)
        {
            if (format.ToLower() == "json")
            {
                setCurrentFormat(LogFormat.Json);
            }
            else if (format.ToLower() == "xml")
            {
                setCurrentFormat(LogFormat.Xml);
            }
        }
    }
}