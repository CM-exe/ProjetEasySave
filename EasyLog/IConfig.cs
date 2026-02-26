namespace EasyLog
{
    /// <summary>
    /// Defines the configuration contract required by the logging system.
    /// Any class implementing this interface must provide paths, formats, and networking details for the logger.
    /// </summary>
    public interface IConfig
    {
        /// <summary>
        /// Retrieves the directory path where local daily log files are stored.
        /// </summary>
        /// <returns>A string representing the log directory path.</returns>
        public string getLogDirectoryPath();

        /// <summary>
        /// Loads or reloads the configuration settings from the underlying physical file (e.g., a JSON config file).
        /// </summary>
        public void loadConfigFile();

        /// <summary>
        /// Retrieves the current global formatting style used for generated logs (e.g., "JSON" or "XML").
        /// </summary>
        /// <returns>A string representing the log format.</returns>
        public string getLogsFormat();

        /// <summary>
        /// Retrieves the specific file path used to track the real-time state of ongoing tasks.
        /// </summary>
        /// <returns>A string representing the real-time log file path.</returns>
        public string getLogRealTimeFile();

        /// <summary>
        /// Retrieves the target IP address of the centralized logging server.
        /// </summary>
        /// <returns>A string representing the server's IP address.</returns>
        public string getServerIp();

        /// <summary>
        /// Retrieves the target port number used to connect to the centralized logging server.
        /// </summary>
        /// <returns>An integer representing the server's port.</returns>
        public int getServerPort();

        /// <summary>
        /// Checks whether the application is configured to send logs over the network to a central server.
        /// </summary>
        /// <returns><c>true</c> if server logging is enabled; otherwise, <c>false</c>.</returns>
        public bool getBoolLogsOnServer();

        /// <summary>
        /// Checks whether the application is configured to persist logs locally on the machine.
        /// </summary>
        /// <returns><c>true</c> if local logging is enabled; otherwise, <c>false</c>.</returns>
        public bool getBoolLogsOnLocal();

        /// <summary>
        /// Updates the formatting style used for future log entries.
        /// </summary>
        /// <param name="newLogsFormat">The new format to apply (e.g., "JSON" or "XML").</param>
        public void setLogsFormat(string newLogsFormat);

        /// <summary>
        /// Updates the IP address of the remote centralized logging server.
        /// </summary>
        /// <param name="newServerIp">The new server IP address.</param>
        public void setServerIp(string newServerIp);

        /// <summary>
        /// Updates the port number of the remote centralized logging server.
        /// </summary>
        /// <param name="newServerPort">The new server port number.</param>
        public void setServerPort(int newServerPort);
    }
}