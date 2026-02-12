using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;


namespace ProjetEasySave.Utils
{
    internal class Config
    {
        private static Config instance = null;
        private static readonly object padlock = new object();

        // Var
        private string configFile;

        // Default variables
        private string defaultLanguage;
        private string defaultLogDirectoryPath;
        private string defaultLogRealTimeFile;
        private string defaultConfigModelsPath;
        private string defaultLogsFormat;
        private string defaultBusinessSoftwareName;

        // Loaded variables
        private string language;
        private string logDirectoryPath;
        private string logRealTimeFile;
        private string configModelsPath;
        private string logsFormat;
        private string businessSoftwareName;

        private Config()
        {
            configFile = Path.Combine(AppContext.BaseDirectory, "../../../config.json");
            defaultLanguage = "en";
            defaultLogDirectoryPath = @"\\localhost\c$\EasyProject\Logs\";
            defaultLogRealTimeFile = Path.Combine(defaultLogDirectoryPath, "real_time_log");
            defaultConfigModelsPath = Path.Combine(AppContext.BaseDirectory, "../../../config_models.json");
            defaultLogsFormat = "json";
            defaultBusinessSoftwareName = "CalculatorApp";
        }

        public static Config Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new Config();
                        // Load config file
                        instance.loadConfigFile();
                    }
                    return instance;
                }
            }
        }

        public void loadConfigFile()
        {
            // Check if file exists
            if (!File.Exists(configFile))
            {
                var Data = new Dictionary<string, string>
                {
                    // Language
                    {"language", new string(defaultLanguage) },
                    {"logDirectoryPath", new string(defaultLogDirectoryPath)},
                    {"logRealTimeFile", new string(defaultLogRealTimeFile)},
                    {"configModelsPath", new string(defaultConfigModelsPath)},
                    {"logsFormat", new string(defaultLogsFormat) },
                    {"businessSoftwareName", new string(defaultBusinessSoftwareName) }
                };
                // Save as JSON object
                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonToSave = JsonSerializer.Serialize(Data, options);

                // Write file
                File.WriteAllText(configFile, jsonToSave);

                // Load var
                language = defaultLanguage;
                logDirectoryPath = defaultLogDirectoryPath;
                logRealTimeFile = defaultLogRealTimeFile;
                configModelsPath = defaultConfigModelsPath;
                logsFormat = defaultLogsFormat;
                businessSoftwareName = defaultBusinessSoftwareName;

            }
            else
            {
                var json = File.ReadAllText(configFile);
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                // Assign variables
                if (dict.ContainsKey("language")) language = dict["language"];
                else language = defaultLanguage;

                if (dict.ContainsKey("logDirectoryPath")) logDirectoryPath = dict["logDirectoryPath"];
                else logDirectoryPath = defaultLogDirectoryPath;

                if (dict.ContainsKey("logRealTimeFile")) logRealTimeFile = dict["logRealTimeFile"];
                else logRealTimeFile = defaultLogRealTimeFile;

                if (dict.ContainsKey("configModelsPath")) configModelsPath = dict["configModelsPath"];
                else configModelsPath = defaultConfigModelsPath;

                if (dict.ContainsKey("logsFormat")) logsFormat = dict["logsFormat"];
                else logsFormat = defaultLogsFormat;

                if (dict.ContainsKey("businessSoftwareName")) businessSoftwareName = dict["businessSoftwareName"];
                else businessSoftwareName = defaultBusinessSoftwareName;
            }

        }

        public void saveConfigFile()
        {
            var Data = new Dictionary<string, string>
            {
                // Language
                {"language", new string(language) },
                {"logDirectoryPath", new string(logDirectoryPath)},
                {"logRealTimeFile", new string(logRealTimeFile)},
                {"configModelsPath", new string(configModelsPath)},
                {"logsFormat", new string(logsFormat) },
                {"businessSoftwareName", new string(businessSoftwareName) }
            };
            // Save as JSON object
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonToSave = JsonSerializer.Serialize(Data, options);
            // Write file
            File.WriteAllText(configFile, jsonToSave);
        }

        // Getters

        public string getLanguage() { return language; }
        public string getLogDirectoryPath() { return logDirectoryPath; }
        public string getLogRealTimeFile() { return logRealTimeFile; }
        public string getLogsFormat() { return logsFormat; }
        public string getConfigModelsPath() { return configModelsPath; }
        public string getBusinessSoftwareName() { return businessSoftwareName; }


        // Setters
        public void setLanguage(string newLanguage) { language = newLanguage; saveConfigFile(); }

        public void setLogsFormat(string newLogsFormat) { logsFormat = newLogsFormat; saveConfigFile(); }
    }
}
