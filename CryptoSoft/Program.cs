using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json.Nodes;
using System.Diagnostics;

namespace CryptoSoft;

public static class Program {
    private const string PIPE_NAME = "EasySave/CryptoSoft";

    public static void Main(string[] args) {
        if (Process.GetProcessesByName("CryptoSoft").Length > 1) Environment.Exit(1);

        List<NamedPipeServerStream> pipes = [];

        // Wait for the first connection
        pipes.Add(_WaitForConnection());

        // Using a task to check if the pipe has no remaining connections and exit if so
        Task.Run(() => {
            while (true) {
                for (int i = pipes.Count - 1; i >= 0; i--) {
                    if (!pipes[i].IsConnected) {
                        pipes.RemoveAt(i);
                        Console.WriteLine("Connection closed");
                    }
                }
                //if (pipes.Count == 0) {
                //    Environment.Exit(0); // Exit if no pipes are connected
                //}
                Thread.Sleep(1000);
            }
        });

        // Continuously wait for new connections
        while (true) {
            pipes.Add(_WaitForConnection());
        }
    }

    private static NamedPipeServerStream _WaitForConnection() {
        NamedPipeServerStream pipeStream = new(PIPE_NAME, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        pipeStream.WaitForConnection();
        Console.WriteLine("New connection established.");
        Task.Run(() => _HandleConnection(pipeStream));
        return pipeStream;
    }

    private static void _HandleConnection(NamedPipeServerStream pipeStream) {
        try {
            using StreamReader reader = new(pipeStream);
            using StreamWriter writer = new(pipeStream) { AutoFlush = true };

            while (pipeStream.IsConnected) {
                try {
                    string? command = reader.ReadLine();
                    if (command is not null) {
                        Console.WriteLine($"Received command: {command}");
                        JsonObject json = JsonNode.Parse(command) as JsonObject ?? throw new InvalidOperationException("Invalid JSON command received.");
                        string fileName = json["FileName"]?.ToString() ?? throw new InvalidOperationException("File name is required.");
                        string key = json["CryptoKey"]?.ToString() ?? throw new InvalidOperationException("Key is required.");

                        if (File.Exists(fileName)) {
                            FileManager file = new(fileName, key);
                            double duration = file.TransformFile();

                            if (duration >= 0) {
                                Console.WriteLine($"File '{fileName}' processed successfully in {duration} ms.");
                            } else {
                                Console.WriteLine($"Error processing file '{fileName}'.");
                            }

                            writer.WriteLine(new JsonObject {
                                ["FileName"] = fileName,
                                ["Duration"] = duration
                            }.ToJsonString());
                        } else {
                            Console.WriteLine($"Error: File '{fileName}' does not exist.");
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"Error processing connection: {ex.Message}");
                }
            }
        } catch { }
    }
}