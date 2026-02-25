using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;
using EasyLog;


namespace ProjetEasySave.Utils
{
    internal class Config: IConfig
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
        private List<string> defaultEncryptionExtensions;
        private string defaultEncryptionKey;
        private string defaultServerIp;
        private string defaultServerPort;
        private string defaultBoolLogsOnServer;
        private string defaultBoolLogsOnLocal;
        private string defaultBiggestSize;

        // Loaded variables
        private string language;
        private string logDirectoryPath;
        private string logRealTimeFile;
        private string configModelsPath;
        private string logsFormat;
        private string businessSoftwareName;
        private List<string> encryptionExtensions;
        private string encryptionKey;
        private string serverIp;
        private string serverPort;
        private string boolLogsOnServer;
        private string boolLogsOnLocal;
        private string biggestSize;

        private Config()
        {
            configFile = Path.Combine(AppContext.BaseDirectory, "../../../config.json");
            defaultLanguage = "en";
            defaultLogDirectoryPath = @"\\localhost\c$\EasyProject\Logs\";
            defaultLogRealTimeFile = Path.Combine(defaultLogDirectoryPath, "real_time_log");
            defaultConfigModelsPath = Path.Combine(AppContext.BaseDirectory, "../../../config_models.json");
            defaultLogsFormat = "json";
            defaultBusinessSoftwareName = "CalculatorApp";
            defaultEncryptionExtensions = new List<string> { ".txt", ".docx", ".jpg", ".png", ".pdf" };
            defaultEncryptionKey = "maCleDeSecurite1234";
            defaultServerIp = "127.0.0.1";
            defaultServerPort = "8080";
            defaultBoolLogsOnLocal = "true";
            defaultBoolLogsOnServer = "true";
            defaultBiggestSize = "10"; // En Ko
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
                    {"businessSoftwareName", new string(defaultBusinessSoftwareName) },
                    {"encryptionExtensions", JsonSerializer.Serialize(defaultEncryptionExtensions)  }, // Ajout de la liste des extensions à chiffrer
                    {"encryptionKey", new string(defaultEncryptionKey) }, // Ajout de la clé de chiffrement
                    {"serverIp", new string(defaultServerIp)  },
                    {"serverPort", new string(defaultServerPort) },
                    {"boolLogsOnLocal", new string(defaultBoolLogsOnLocal)  },
                    {"boolLogsOnServer", new string(defaultBoolLogsOnServer) },
                    {"biggestSize", new string(defaultBiggestSize) },
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
                encryptionExtensions = defaultEncryptionExtensions;
                encryptionKey = defaultEncryptionKey;
                serverIp = defaultServerIp;
                serverPort = defaultServerPort;
                boolLogsOnServer = defaultBoolLogsOnServer;
                boolLogsOnLocal = defaultBoolLogsOnLocal;
                biggestSize = defaultBiggestSize;
                saveConfigFile();

            }
            else
            {
                var json = File.ReadAllText(configFile);

                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

                // Assignation des variables simples (on utilise .ToString())
                if (dict.ContainsKey("language")) language = dict["language"].ToString();
                else language = defaultLanguage;

                if (dict.ContainsKey("logDirectoryPath")) logDirectoryPath = dict["logDirectoryPath"].ToString();
                else logDirectoryPath = defaultLogDirectoryPath;

                if (dict.ContainsKey("logRealTimeFile")) logRealTimeFile = dict["logRealTimeFile"].ToString();
                else logRealTimeFile = defaultLogRealTimeFile;

                if (dict.ContainsKey("configModelsPath")) configModelsPath = dict["configModelsPath"].ToString();
                else configModelsPath = defaultConfigModelsPath;

                if (dict.ContainsKey("logsFormat")) logsFormat = dict["logsFormat"].ToString();
                else logsFormat = defaultLogsFormat;

                if (dict.ContainsKey("businessSoftwareName")) businessSoftwareName = dict["businessSoftwareName"].ToString();
                else businessSoftwareName = defaultBusinessSoftwareName;

                // GESTION DU CHIFFREMENT (Clé)
                if (dict.ContainsKey("encryptionKey")) encryptionKey = dict["encryptionKey"].ToString();
                else encryptionKey = defaultEncryptionKey;

                // GESTION DU CHIFFREMENT (Liste)
                if (dict.ContainsKey("encryptionExtensions"))
                {
                    try
                    {
                        encryptionExtensions = JsonSerializer.Deserialize<List<string>>(dict["encryptionExtensions"].GetRawText());
                    }
                    catch
                    {
                        encryptionExtensions = defaultEncryptionExtensions;
                    }
                }
                else
                {
                    encryptionExtensions = defaultEncryptionExtensions;
                }

                if (dict.ContainsKey("serverIp")) serverIp = dict["serverIp"].ToString();
                else serverIp = defaultServerIp;

                if (dict.ContainsKey("serverPort")) serverPort = dict["serverPort"].ToString();
                else serverPort = defaultServerPort;

                if (dict.ContainsKey("boolLogsOnServer")) boolLogsOnServer = dict["boolLogsOnServer"].ToString();
                else boolLogsOnServer = defaultBoolLogsOnServer;

                if (dict.ContainsKey("boolLogsOnLocal")) boolLogsOnLocal = dict["boolLogsOnLocal"].ToString();
                else boolLogsOnLocal = defaultBoolLogsOnLocal;

                if (dict.ContainsKey("biggestSize")) biggestSize = dict["biggestSize"].ToString();
                else biggestSize = defaultBiggestSize;
            }

        }

        public void saveConfigFile()
        {
            var Data = new
            {
                language = language,
                logDirectoryPath = logDirectoryPath,
                logRealTimeFile = logRealTimeFile,
                configModelsPath = configModelsPath,
                logsFormat = logsFormat,
                businessSoftwareName = businessSoftwareName,
                encryptionKey = encryptionKey, // Ajout de la clé
                encryptionExtensions = encryptionExtensions, // Ajout de la liste (sera sauvegardée comme tableau [])
                serverIp = serverIp,
                serverPort = serverPort,
                boolLogsOnServer = boolLogsOnServer,
                boolLogsOnLocal = boolLogsOnLocal,
                biggestSize = biggestSize
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonToSave = JsonSerializer.Serialize(Data, options);

            File.WriteAllText(configFile, jsonToSave);
        }

        // Getters

        public string getLanguage() { return language; }
        public string getLogDirectoryPath() { return logDirectoryPath; }
        public string getLogRealTimeFile() { return logRealTimeFile; }
        public string getLogsFormat() { return logsFormat; }
        public string getConfigModelsPath() { return configModelsPath; }
        public string getBusinessSoftwareName() { return businessSoftwareName; }
        public List<string> getEncryptionExtensions() { return encryptionExtensions; }
        public string getEncryptionKey() { return encryptionKey; }
        public string getServerIp() { return serverIp; }
        public int getServerPort() { return int.Parse(serverPort); }
        public bool getBoolLogsOnServer() { return bool.Parse(boolLogsOnServer); }
        public bool getBoolLogsOnLocal() { return bool.Parse(boolLogsOnLocal); }
        public int getBiggestSize() { return int.Parse(biggestSize); }


        // Setters
        public void setLanguage(string newLanguage) { language = newLanguage; saveConfigFile(); }

        public void setLogsFormat(string newLogsFormat) { logsFormat = newLogsFormat; saveConfigFile(); }

        public void setServerIp(string newServerIp) { serverIp = newServerIp; saveConfigFile(); }

        public void setServerPort(int newServerPort) { serverPort = newServerPort.ToString(); saveConfigFile(); }
        public void setBoolLogsOnServer(bool newBoolLogsOnServer) { boolLogsOnServer = newBoolLogsOnServer.ToString(); saveConfigFile(); }
        public void setBoolLogsOnLocal(bool newBoolLogsOnLocal) { boolLogsOnLocal = newBoolLogsOnLocal.ToString(); saveConfigFile(); }
        public void setBiggestSize(int newBiggestSize) { biggestSize = newBiggestSize.ToString(); saveConfigFile(); }
        public void setEncryptionKey(string newEncryptionKey) { encryptionKey = newEncryptionKey; saveConfigFile(); }
    }
}
