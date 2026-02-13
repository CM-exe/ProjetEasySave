using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace EasySave.Logger;

// Interface defining methods for managing log files
public interface ILogFile {
    // Method to save a log entry to a file
    public void Save(Log log, string filepath);

    // Method to read logs from a file
    public List<Log> Read(string filepath);
}

// Implementation of the ILogFile interface
public class LogFileXML : ILogFile {
    private readonly object _LockObject = new(); // Lock object for thread safety

    // Saves a Log object to a XML file
    public void Save(Log log, string filePath) {
        lock (_LockObject) { // Ensure thread safety when writing to the file
                             // Create a JSON object from the log properties
            XElement newLog = new("log",
                new XElement("DateTime", log.Datetime),
                new XElement("Name", log.JobName),
                new XElement("Destination", log.Destination),
                new XElement("Source", log.Source),
                new XElement("TaskType", log.TaskType),
                new XElement("Filesize", log.Filesize),
                new XElement("TransfertDuration", log.TransfertDuration),
                new XElement("Level", log.Level.ToString()),
                new XElement("Message", log.Message)
            );


            string xmlString = newLog.ToString();

            StreamWriter file = File.AppendText(filePath); // Open the file for appending
            file.WriteLine(xmlString); // Write the XML string to the file
        }
    }
    // Reads the content of a JSON file and returns a list of Log objects


    public List<Log> Read(string filePath) {
        lock (_LockObject) { // Ensure thread safety when reading the file
                             // Vérifie si le fichier existe
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException("XML file not found.");
            }

            try {
                string xmlContent = File.ReadAllText(filePath);

                // Return an empty list if the file is empty or contains only whitespace
                if (string.IsNullOrWhiteSpace(xmlContent)) {
                    return [];
                }

                List<Log> logs = [];

                foreach (string line in xmlContent.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)) {
                    // Parse each line as a JSON object
                    XElement? xmlNode = XElement.Parse(line);
                    if (xmlNode != null) {
                        Log log = new() {
                            Datetime = DateTime.Parse(xmlNode.Element("DateTime")?.Value ?? DateTime.MinValue.ToString()),
                            JobName = xmlNode.Element("Name")?.Value ?? "",
                            Source = xmlNode.Element("Source")?.Value ?? "",
                            Destination = xmlNode.Element("Destination")?.Value ?? "",
                            TaskType = xmlNode.Element("TaskType")?.Value ?? "",
                            Filesize = double.TryParse(xmlNode.Element("Filesize")?.Value, out double size) ? size : 0,
                            TransfertDuration = double.TryParse(xmlNode.Element("TransfertDuration")?.Value, out double dur) ? dur : 0,
                            Level = Enum.TryParse<LogLevel>(xmlNode.Element("Level")?.Value, out var level) ? level : LogLevel.Information,
                            Message = xmlNode.Element("Message")?.Value ?? ""
                        };
                        logs.Add(log);
                    }
                }

                return logs;
            } catch (Exception ex) {
                throw new Exception(filePath + " : " + ex.Message);
            }
        }
    }
}

public class LogFileJSON : ILogFile { // Saves a Log object to a JSON file

    private readonly object _LockObject = new(); // Lock object for thread safety

    public void Save(Log log, string filePath) {
        lock (_LockObject) { // Ensure thread safety when writing to the file
            // Create a JSON object from the log properties
            var jsonObject = new JsonObject {
                ["DateTime"] = log.Datetime,
                ["Name"] = log.JobName,
                ["Destination"] = log.Destination,
                ["Source"] = log.Source,
                ["TaskType"] = log.TaskType,
                ["Filesize"] = log.Filesize,
                ["TransfertDuration"] = log.TransfertDuration,
                ["Level"] = log.Level.ToString(),
                ["Message"] = log.Message
            };

            // Convert the JSON object to a string
            string jsonString = jsonObject.ToJsonString();
            try {
                using StreamWriter file = File.AppendText(filePath); // Open the file for appending
                file.WriteLine(jsonString); // Write the JSON string to the file
            } catch (IOException) {
                // Oh No!
                // Anyway
            }
        }
    }

    // Reads the content of a JSON file and returns a list of Log objects
    public List<Log> Read(string filePath) {
        // Check if the file exists
        lock (_LockObject) {
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException("JSON file not found.");
            }

            try {
                string jsonContent = File.ReadAllText(filePath);

                // Return an empty list if the file is empty or contains only whitespace
                if (string.IsNullOrWhiteSpace(jsonContent)) {
                    return [];
                }

                List<Log> logs = [];

                foreach (string line in jsonContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) {
                    // Parse each line as a JSON object
                    JsonNode? jsonNode = JsonNode.Parse(line);
                    if (jsonNode is JsonObject jsonObject) {
                        Log log = new() {
                            Datetime = DateTime.Parse(jsonObject["DateTime"]?.ToString() ?? DateTime.MinValue.ToString()),
                            JobName = jsonObject["Name"]?.ToString() ?? "",
                            Source = jsonObject["Source"]?.ToString() ?? "",
                            Destination = jsonObject["Destination"]?.ToString() ?? "",
                            TaskType = jsonObject["TaskType"]?.ToString() ?? "",
                            Filesize = double.TryParse(jsonObject["Filesize"]?.ToString(), out double size) ? size : 0,
                            TransfertDuration = double.TryParse(jsonObject["TransfertDuration"]?.ToString(), out double dur) ? dur : 0,
                            Level = Enum.TryParse<LogLevel>(jsonObject["Level"]?.ToString(), out var level) ? level : LogLevel.Information,
                            Message = jsonObject["Message"]?.ToString() ?? "",
                        };
                        logs.Add(log);
                    }
                }

                return logs;
            } catch (Exception ex) {
                // Handle errors during file reading or deserialization
                throw new Exception(filePath + " : " + ex.Message);
            }
        }
    }

    public void Clear(string filePath) {
        lock(_LockObject) {
            File.WriteAllText(filePath, "");
        }
    }
}

