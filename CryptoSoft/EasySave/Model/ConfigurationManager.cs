using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EasySave.Model;

public interface IConfigurationManager {
    /// <summary>
    /// Creates a new instance of ConfigurationManager
    /// </summary>
    static IConfigurationManager? Instance { get; }

    /// <summary>
    /// Method to load a configuration file
    /// </summary>
    IConfiguration Load(string filePath);
    /// <summary>
    /// Method to save a configuration file
    /// </summary>
    void Save(string filePath, IConfiguration configuration);
}

public class ConfigurationManager : IConfigurationManager {
    /// <summary>
    /// Instance of the ConfigurationManager
    /// </summary>
    public static ConfigurationManager? Instance { get; private set; }
    private readonly Type Loader;
    /// <summary>
    /// Current configuration
    /// </summary>
    public IConfiguration? Configuration { get; set; }

    /// <summary>
    /// Singleton instance of ConfigurationManager
    /// </summary>
    public ConfigurationManager(Type loader) {
        // Check if the instance is already created
        if (ConfigurationManager.Instance != null) {
            throw new InvalidOperationException("ConfigurationManager is a singleton. Use Instance property to access it.");
        }
        // Check if the loader is null
        if (!typeof(IConfigurationFile).IsAssignableFrom(loader)){
            throw new ArgumentException("Loader must implement IConfigurationFile", nameof(loader));
        }
        // assign the loader
        this.Loader = loader;
        // assign the instance
        ConfigurationManager.Instance = this;
    }

    /// <summary>
    /// Load a configuration file
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public IConfiguration Load(string filePath) {
        IConfigurationFile file = (IConfigurationFile)Activator.CreateInstance(this.Loader, filePath)!;
        IConfiguration configuration = file.Read() ?? throw new InvalidOperationException("Configuration is null");
        // EVENT: ConfigurationChanged
        // Subscribe to the ConfigurationChanged event
        configuration.ConfigurationChanged += (sender, e) => {
            OnConfigurationChanged(filePath);
        };

        this.Configuration = configuration;

        return configuration;
    }

    // Save the configuration to the file
    public void Save(string filePath, IConfiguration configuration) {
        // Get the configuration file
        IConfigurationFile file = (IConfigurationFile)Activator.CreateInstance(this.Loader, filePath)!;
        // Save the configuration to the file
        file.Save(configuration);
    }

    // When the configuration is changed, save it to the file
    private void OnConfigurationChanged(string filePath) {
        this.Save(filePath, this.Configuration!);
    }
}
