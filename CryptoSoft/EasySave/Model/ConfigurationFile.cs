using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;


namespace EasySave.Model;

public class ConfigurationDTO {
    public string Language { get; set; } = IConfiguration.DEFAULT_LANGUAGE;
    public string StateFile { get; set; } = IConfiguration.DEFAULT_STATE_FILE;
    public string LogFile { get; set; } = IConfiguration.DEFAULT_LOG_FILE;
    public string CryptoFile { get; set; } = IConfiguration.DEFAULT_CRYPTO_FILE;
    public string CryptoKey { get; set; } = IConfiguration.DEFAULT_CRYPTO_KEY;
    public List<string> CryptoExtensions { get; set; } = [];
    public List<string> Processes { get; set; } = [];
    public List<BackupJobConfigurationDTO> Jobs { get; set; } = [];
    public List<string> PriorityExtensions { get; set; } = [];
    public int MaxConcurrentJobs { get; set; } = IConfiguration.DEFAULT_MAX_CONCURRENT_JOBS;
    public int MaxConcurrentSize { get; set; } = IConfiguration.DEFAULT_MAX_CONCURRENT_SIZE;
}

public interface IConfigurationFile {
    /// <summary>
    /// Method to save the configuration to a file
    /// </summary>
    void Save(IConfiguration configuration);
    /// <summary>
    /// Method to read the configuration from a file
    /// </summary>
    IConfiguration Read();
}

public class ConfigurationJSONFile(string filePath) : IConfigurationFile {
    // set the file path
    private string _FilePath { get; set; } = filePath;
    private object _LockObject = new();

    /// <summary>
    /// Save the configuration to a file
    /// </summary>
    /// param name="configuration"></param>
    /// returns></returns>
    public void Save(IConfiguration configuration) {
        var jsonObject = configuration.ToJSON();

        // put the state file in the json object
        string jsonString = jsonObject.ToJsonString(new JsonSerializerOptions {
            WriteIndented = true
        });

        lock (this._LockObject) {
            // write the json object to the file
            using StreamWriter writer = new(this._FilePath);
            writer.Write(jsonString);
        }
    }

    /// <summary>
    /// Read the configuration from a file
    /// </summary>
    /// <returns>
    /// IConfiguration
    /// </returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IConfiguration Read() {
        bool isNew = false;
        // check if the file exists
        if (!File.Exists(this._FilePath)) {
            lock (this._LockObject) {
                // if the file does not exist, create it
                using StreamWriter writer = new(this._FilePath);
                writer.Write("{}");
            }
            isNew = true;
        }

        // read the file
        string json = string.Empty;
        lock (this._LockObject) {
            json = File.ReadAllText(this._FilePath);
        }

        // Parse the JSON string into a ConfigurationDTO object
        ConfigurationDTO? configDto = JsonSerializer.Deserialize<ConfigurationDTO>(json) ?? throw new InvalidOperationException("The JSON content is not a valid ConfigurationDTO.");

        // Create the IConfiguration instance from the DTO
        IConfiguration configuration = new Configuration(configDto);

        // Save the configuration if it is new
        if (isNew) {
            // Save the configuration to the file
            this.Save(configuration);
        }

        return configuration;
    }
}