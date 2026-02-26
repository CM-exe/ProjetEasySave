namespace EasyLog
{
    public interface IConfig
    {
        public string getLogDirectoryPath();
        public void loadConfigFile();
        public string getLogsFormat();
        public string getLogRealTimeFile();
        public string getServerIp();
        public int getServerPort();
        public bool getBoolLogsOnServer();
        public bool getBoolLogsOnLocal();
        public void setLogsFormat(string newLogsFormat);
        public void setServerIp(string newServerIp);
        public void setServerPort(int newServerPort);
    }
}