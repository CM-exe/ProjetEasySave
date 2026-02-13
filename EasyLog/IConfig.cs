namespace EasyLog
{
    public interface IConfig
    {
        public string getLogDirectoryPath();
        public void loadConfigFile();
        public string getLogsFormat();
        public string getLogRealTimeFile();
        public void setLogsFormat(string newLogsFormat);

    }
}