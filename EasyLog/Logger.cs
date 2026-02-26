using System.Text;
using System.Net.Sockets;
using System.Net;

namespace EasyLog
{
    /// <summary>
    /// Enumeration to define the supported log formats.
    /// </summary>
    public enum LogFormat
    {
        /// <summary>JavaScript Object Notation format.</summary>
        Json,
        /// <summary>Extensible Markup Language format.</summary>
        Xml
    }

    /// <summary>
    /// Handles the generation, formatting, and dispatching of application logs.
    /// Supports writing to local files and streaming to a remote centralized logging server via TCP sockets.
    /// Implements the Singleton pattern to ensure a single logging stream across the application.
    /// </summary>
    public class Logger
    {
        // Attributes
        private static Logger singletonInstance;
        private string logDirectoryPath;
        private string logRealTimeFile;
        private static IConfig _config; // Load config
        private static Socket loggingServerSocket; // Socket for logging server connection
        private static readonly object _lockSingleton = new object();

        // Property to define the current format (adjustable by the user)
        private LogFormat currentFormat = LogFormat.Json;

        /// <summary>
        /// Prevents a default instance of the <see cref="Logger"/> class from being created.
        /// Initializes the log directories, fetches the user's preferred format from the configuration, 
        /// and attempts to establish a connection to the remote logging server.
        /// </summary>
        /// <param name="config">The configuration provider implementing <see cref="IConfig"/>.</param>
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

        /// <summary>
        /// Retrieves the thread-safe singleton instance of the <see cref="Logger"/>.
        /// </summary>
        /// <param name="config">The configuration provider to use if the logger needs to be initialized.</param>
        /// <returns>The single active instance of the <see cref="Logger"/>.</returns>
        public static Logger getInstance(IConfig config)
        {
            lock (_lockSingleton)
            {
                if (singletonInstance == null)
                {
                    singletonInstance = new Logger(config);
                }
                return singletonInstance;
            }
        }

        // --- Historical Logging Method (Append mode) ---

        /// <summary>
        /// Records a historical log entry. It appends the log to a local daily file and/or sends it to the remote server, 
        /// depending on the active configuration settings.
        /// </summary>
        /// <param name="message">A dictionary containing the key-value pairs representing the log data.</param>
        /// <returns><c>true</c> if the log was successfully recorded/sent everywhere it was supposed to go; otherwise, <c>false</c>.</returns>
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

            if (loggingServerSocket != null && _config.getBoolLogsOnServer())
            {
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

        /// <summary>
        /// Establishes a new TCP socket connection to the logging server using the IP and Port defined in the configuration.
        /// </summary>
        /// <returns>An active <see cref="Socket"/> connected to the server.</returns>
        private static Socket connectToServer()
        {
            // Create a new socket for the client
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Connect to the server using the config values for IP and port
            socket.Connect(new IPEndPoint(IPAddress.Parse(_config.getServerIp()), _config.getServerPort()));
            return socket;
        }

        /// <summary>
        /// Safely shuts down and closes an active socket connection.
        /// </summary>
        /// <param name="socket">The <see cref="Socket"/> to disconnect.</param>
        private static void disconnectFromServer(Socket socket)
        {
            if (socket != null && socket.Connected)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        /// <summary>
        /// Forces a disconnection and attempts to establish a new connection to the remote logging server.
        /// </summary>
        /// <returns><c>true</c> if the reconnection was successful; otherwise, <c>false</c>.</returns>
        public bool reconnectToServer()
        {
            try
            {
                disconnectFromServer(loggingServerSocket);
                loggingServerSocket = connectToServer();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to reconnect to logging server: " + ex.Message);
                loggingServerSocket = null; // Set to null if connection fails
                return false;
            }
        }

        /// <summary>
        /// Checks whether the logger currently has an active TCP connection to the remote server.
        /// </summary>
        /// <returns><c>true</c> if the socket is connected; otherwise, <c>false</c>.</returns>
        public bool isConnectedToServer()
        {
            return loggingServerSocket != null && loggingServerSocket.Connected;
        }

        /// <summary>
        /// Sends a raw string message over the provided TCP socket using UTF8 encoding.
        /// </summary>
        /// <param name="socket">The active socket connection.</param>
        /// <param name="message">The formatted string payload to transmit.</param>
        private static void sendMessageToServer(Socket socket, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            socket.Send(data);
        }

        // --- Real-Time Logging Method (Overwrite mode) ---

        /// <summary>
        /// Records a real-time log entry by completely overwriting the target file.
        /// This is used to maintain a constant "current state" of the application for external monitoring tools.
        /// </summary>
        /// <param name="message">A dictionary containing the state data.</param>
        /// <returns><c>true</c> if the file was successfully overwritten; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Converts a dictionary of log data into a valid JSON string object.
        /// Automatically injects the current Windows user into the payload.
        /// </summary>
        /// <param name="message">The dictionary of log entries.</param>
        /// <returns>A formatted JSON string.</returns>
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

        /// <summary>
        /// Converts a dictionary of log data into a valid XML `<logEntry>` block.
        /// Automatically injects the current Windows user into the payload.
        /// </summary>
        /// <param name="message">The dictionary of log entries.</param>
        /// <returns>A formatted XML string.</returns>
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

        /// <summary>
        /// Appends a formatted string block into a local file, intelligently handling JSON array brackets 
        /// `[]` or XML root tags `<root>` to ensure the entire file remains validly formatted over time.
        /// </summary>
        /// <param name="path">The absolute path to the local log file.</param>
        /// <param name="content">The formatted string block to insert.</param>
        /// <param name="format">The current <see cref="LogFormat"/> being used.</param>
        /// <returns><c>true</c> if successfully written; otherwise, <c>false</c>.</returns>
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

        /// <summary>
        /// Formats the raw data of a real-time state change into a dictionary structure suitable for logging.
        /// </summary>
        /// <param name="name">The name of the backup job.</param>
        /// <param name="source">The source file path.</param>
        /// <param name="target">The target file path.</param>
        /// <param name="time">The timestamp of the event.</param>
        /// <param name="saveTaskState">The dynamic state of the task (e.g., RUNNING, PAUSED).</param>
        /// <returns>A dictionary containing the structured data.</returns>
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

        /// <summary>
        /// Formats the raw data of a completed file transfer into a dictionary structure suitable for logging.
        /// </summary>
        /// <param name="name">The name of the backup job.</param>
        /// <param name="source">The source file path.</param>
        /// <param name="target">The target file path.</param>
        /// <param name="size">The size of the transferred file in bytes.</param>
        /// <param name="transferTime">The time taken to copy the file in milliseconds.</param>
        /// <param name="time">The timestamp of the completion.</param>
        /// <returns>A dictionary containing the structured data.</returns>
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

        /// <summary>
        /// Formats the summary data of a fully completed backup job into a dictionary structure suitable for logging.
        /// </summary>
        /// <param name="name">The name of the backup job.</param>
        /// <param name="source">The root source directory.</param>
        /// <param name="target">The root target directory.</param>
        /// <param name="time">The timestamp of completion.</param>
        /// <returns>A dictionary containing the structured data.</returns>
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

        /// <summary>
        /// Formats an application exception or error message into a dictionary structure suitable for logging.
        /// </summary>
        /// <param name="errorMessage">The raw error string or exception message.</param>
        /// <returns>A dictionary containing the error string and a timestamp.</returns>
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

        /// <summary>Gets the current <see cref="LogFormat"/> enum value being used by the logger.</summary>
        /// <returns>The active log format.</returns>
        public LogFormat getCurrentFormat()
        {
            return currentFormat;
        }

        /// <summary>
        /// Sets the <see cref="LogFormat"/> enum and persists the change to the global configuration file.
        /// </summary>
        /// <param name="format">The new <see cref="LogFormat"/> to apply.</param>
        public void setCurrentFormat(LogFormat format)
        {
            currentFormat = format;
            _config.setLogsFormat(format == LogFormat.Json ? "json" : "xml"); // Save the new format in the config file
        }

        /// <summary>
        /// Retrieves the current format as a string (useful for UI data binding).
        /// </summary>
        /// <returns>A string representation of the format (e.g., "Json" or "Xml").</returns>
        public string getLogsFormat()
        {
            return currentFormat.ToString();
        }

        /// <summary>
        /// Attempts to parse a string input into a <see cref="LogFormat"/> enum and updates the logger.
        /// </summary>
        /// <param name="format">A string representing the format ("json" or "xml", case-insensitive).</param>
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