using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace EasySave.Model;

public interface ICrypto {
    /// <summary>
    /// Crypts the file at the specified path using the crypto executable.
    /// Waits for the process to complete and returns the duration in milliseconds.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    double Crypt(string filePath);

    static abstract ICrypto Acquire();
    void Release();
}

internal class CryptoResponseEventArgs : EventArgs {
    public required string Response { get; set; }
}

public class Crypto : ICrypto {
    private const string PIPE_NAME = "EasySave/CryptoSoft";

    private static Crypto? _Instance { get; set; }
    private static int _AcquireCount { get; set; } = 0;
    private static NamedPipeClientStream? _PipeClient { get; set; } = null;
    private static StreamReader? _PipeReader { get; set; } = null;
    private static StreamWriter? _PipeWriter { get; set; } = null;
    private static bool _IsPipeConnected => _PipeClient?.IsConnected ?? false;
    private static Task? _PipeReaderTask { get; set; } = null;
    private static readonly object _Lock = new();

    private string _ExecutablePath { get; set; }
    private string _CryptoKey { get; set; }
    public Crypto(string cryptoFile, string cryptoKey) {
        if (Configuration.Instance is null) {
            throw new InvalidOperationException("Configuration instance is not initialized.");
        }
        _ExecutablePath = cryptoFile;
        _CryptoKey = cryptoKey;

        _Instance = this;
    }

    public double Crypt(string filePath) {
        return _SendCommand(filePath);
    }

    public void Dispose() {
        _DisconnectNamedPipe();

        _Instance = null;
    }

    ~Crypto() {
        Dispose();
    }

    public static ICrypto Acquire() {
        lock (_Lock) {
            if (_Instance is null) {
                throw new InvalidOperationException("Crypto instance is not initialized. Call constructor first");
            }
            _AcquireCount++;

            if (_AcquireCount == 1) {
                _ConnectNamedPipe();
            }

            return _Instance;
        }
    }

    public void Release() {
        lock (_Lock) {
            _AcquireCount--;

            if (_AcquireCount == 0) {
                _DisconnectNamedPipe();
            }
        }
    }

    private static void _ConnectNamedPipe() {
        _StartCryptoSoftIfNot();
        _PipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.InOut, PipeOptions.Asynchronous);
        _PipeClient.Connect(5000); // Wait for 5 seconds to connect to the pipe
        _PipeReader = new StreamReader(_PipeClient);
        _PipeWriter = new StreamWriter(_PipeClient) { AutoFlush = true };

        _PipeReaderTask = Task.Run(() => {
            while (_IsPipeConnected) {
                try {
                    string? line = _PipeReader.ReadLine();
                    if (line is not null) {
                        _ResponseReceived?.Invoke(null, new CryptoResponseEventArgs { Response = line });
                    }
                } catch { }
            }
        });
    }

    private static void _StartCryptoSoftIfNot() {
        List<Process> processes = [.. Process.GetProcessesByName("CryptoSoft")];
        if (processes.Count == 0) {
            Process.Start(_Instance!._ExecutablePath);
            Process process = new() {
                StartInfo = new ProcessStartInfo {
                    FileName = _Instance!._ExecutablePath,
                    CreateNoWindow = true
                }
            };

            try {
                process.Start();
            } catch (Exception ex) {
                Debug.WriteLine($"Failed to start CryptoSoft: {ex.Message}");
                throw new InvalidOperationException("Failed to start CryptoSoft. Ensure the executable path is correct.", ex);
            }
        }
    }

    private static void _DisconnectNamedPipe() {
        // Close the pipe client if it is connected
        _PipeClient?.Close();

        // Wait for the pipe reader task to complete before disposing of it
        _PipeReaderTask?.Wait();

        // Reset static fields
        _PipeClient = null;
        _PipeReader = null;
        _PipeWriter = null;
        _PipeReaderTask = null;
    }

    private static bool _IsEverythingOk() {
        return _Instance is not null &&
               _PipeClient is not null &&
               _PipeReader is not null &&
               _PipeWriter is not null &&
               _IsPipeConnected;
    }

    private static bool _TryRepairPipe() {
        if (_IsEverythingOk()) {
            return true; // Pipe is already connected and ready
        }

        try {
            _DisconnectNamedPipe(); // Ensure we clean up any previous state
            _ConnectNamedPipe(); // Attempt to reconnect
            return true;
        } catch (Exception ex) {
            Debug.WriteLine($"Failed to repair pipe: {ex.Message}");
            return false;
        }
    }

    private static double _SendCommand(string fileName) {
        if (!_TryRepairPipe()) {
            throw new InvalidOperationException("Failed to connect to the crypto service. Ensure CryptoSoft is running and the pipe is available.");
        }

        double duration = -1;
        ManualResetEventSlim waitHandle = new(false);

        EventHandler<CryptoResponseEventArgs> _OnResponseReceived = new((sender, e) => {
            JsonObject json = JsonNode.Parse(e.Response) as JsonObject ?? throw new InvalidOperationException("Invalid JSON response received.");
            if (json["FileName"]?.ToString() == fileName) {
                if (!double.TryParse(json["Duration"]?.ToString(), out double parsedDuration)) {
                    throw new InvalidOperationException("Invalid duration value in response.");
                }
                duration = parsedDuration;
                waitHandle.Set();
            }
        });

        _ResponseReceived += _OnResponseReceived;

        _PipeWriter!.WriteLine(new JsonObject() {
            ["FileName"] = fileName,
            ["CryptoKey"] = _Instance!._CryptoKey
        }.ToJsonString()); // Using ToJsonString() to ensure proper serialization)

        waitHandle.Wait(TimeSpan.FromSeconds(2));

        _ResponseReceived -= _OnResponseReceived;

        return duration;
    }

    private static event EventHandler<CryptoResponseEventArgs>? _ResponseReceived;
}
