using EasySave.Helpers;
using EasySave.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace EasySave.Model;

public class SocketCommand : Command {
    public Socket? Client { get; set; }

    public override SocketCommand Clone() {
        return new SocketCommand() {
            Name = this.Name,
            Arguments = [.. this.Arguments],
            ArgumentsParser = this.ArgumentsParser,
            Handler = this.Handler,
            Client = this.Client
        };
    }
}

public class SocketServer {
    private readonly int PORT = 8080;
    private readonly IPAddress HOST = IPAddress.Any;

    private readonly IViewModel _ViewModel;

    private Socket? _Socket { get; set; }
    private readonly Dictionary<string, Socket> _Clients = [];

    public SocketServer(IViewModel viewModel) {
        this._ViewModel = viewModel;

        this._ViewModel.Commands.RegisterCommand(new SocketCommand() {
            Name = "list-json",
            Handler = (command) => {
                if (command is SocketCommand socketCommand) {
                    JsonArray jsonArray = [];
                    for (int i = 0; i < this._ViewModel.Configuration.Jobs.Count; i++) {
                        IBackupJobConfiguration job = this._ViewModel.Configuration.Jobs[i];
                        JsonObject jsonObject = new() {
                            ["Name"] = job.Name,
                            ["Source"] = job.Source,
                            ["Destination"] = job.Destination,
                            ["Type"] = job.Type
                        };
                        jsonArray.Add(jsonObject);
                    }
                    socketCommand.Client!.Send(Encoding.UTF8.GetBytes("JOBS|" + jsonArray.ToJsonString() + "\n"));
                } else {
                    this._ReportError("Invalid command type. Expected SocketCommand.", null);
                }
            },
        });

        this._ViewModel.Commands.RegisterCommand(new SocketCommand() {
            Name = "running",
            Handler = (command) => {
                if (command is SocketCommand socketCommand) {
                    socketCommand.Client!.Send(Encoding.UTF8.GetBytes("STATE|" + (this._ViewModel.BackupState?.ToJSON(false) ?? "[]") + "\n"));
                } else {
                    this._ReportError("Invalid command type. Expected SocketCommand.", null);
                }
            },
        });

        this._ViewModel.Commands.RegisterCommand(new SocketCommand() {
            Name = "configuration",
            Handler = (command) => {
                if (command is SocketCommand socketCommand) {
                    socketCommand.Client!.Send(Encoding.UTF8.GetBytes("CONFIG|" + this._ViewModel.Configuration.ToString() + "\n"));
                } else {
                    this._ReportError("Invalid command type. Expected SocketCommand.", null);
                }
            }
        });

        this._StartServer();
        this._StartAcceptingConnections();
    }

    private void _StartServer() {
        IPEndPoint iPEndPoint = new(HOST, PORT);
        this._Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        this._Socket.Bind(iPEndPoint);
        this._Socket.Listen(10);
        this._Log($"Server started on {HOST}:{PORT}");
    }

    private void _StartAcceptingConnections() {
        Task.Run(() => {
            while (true) {
                try {
                    Socket clientSocket = this._AcceptConnection();
                    this._StartStateUpdateLoop(clientSocket);
                    Task.Run(() => this._HandleClient(clientSocket));
                } catch (Exception ex) {
                    this._ReportError($"Error accepting connection: {ex.Message}");
                }
            }
        });
    }

    private Socket _AcceptConnection() {
        try {
            Socket clientSocket = this._Socket!.Accept();
            this._Log($"Client connected: {clientSocket.RemoteEndPoint}");
            return clientSocket;
        } catch (SocketException ex) {
            this._ReportError($"Socket error: {ex.Message}", null);
            throw;
        } catch (Exception ex) {
            this._ReportError($"Error accepting connection: {ex.Message}", null);
            throw;
        }
    }

    private void _Log(string message) {
        this._ViewModel.Logger.Info(new Log() {
            Message = message
        });
    }

    private void _ReportError(string error, Socket? client = null) {
        this._ViewModel.Logger.Error(new Log() {
            Message = error
        });

        if (client != null) {
            this._SendError(client, error);
        }
    }

    private void _SendError(Socket client, string error) {
        try {
            client.Send(Encoding.UTF8.GetBytes($"ERROR|{error}\n"));
        } catch (SocketException ex) {
            this._ViewModel.Logger.Error(new Log() {
                Message = $"Socket error: {ex.Message}"
            });
            client.Close();
        } catch (ObjectDisposedException) {
            this._ViewModel.Logger.Error(new Log() {
                Message = $"Socket closed"
            });
        }
    }

    private void _HandleClient(Socket clientSocket) {
        while (true) {
            try {
                byte[] buffer = new byte[1024];
                int receivedBytes = clientSocket.Receive(buffer);
                if (receivedBytes == 0) break; // Client disconnected
                string commandLine = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                this._Log($"Received command: {commandLine}");

                int pipeIndex = commandLine.IndexOf('|');
                Command? command = this._ViewModel.Commands.GetCommand(pipeIndex == -1 ? commandLine.Trim() : commandLine.Substring(0, pipeIndex).Trim());
                string arguments = pipeIndex == -1 ? string.Empty : commandLine[(pipeIndex + 1)..].Trim();
                if (command is SocketCommand socketCommand) {
                    socketCommand.Client = clientSocket;
                    socketCommand.Arguments = socketCommand.ArgumentsParser(arguments);
                    this._ViewModel.Commands.RunCommand(socketCommand);
                } else {
                    command.Arguments = command.ArgumentsParser(arguments);
                    this._ViewModel.Commands.RunCommand(command);
                }
            } catch (SocketException ex) {
                this._ReportError($"Socket error: {ex.Message}", clientSocket);
                break;
            } catch (Exception ex) {
                this._ReportError($"Error handling client: {ex.Message}", clientSocket);
            }
        }

        try {
            clientSocket.Shutdown(SocketShutdown.Both);
        } catch (SocketException ex) {
            this._ReportError($"Socket error during shutdown: {ex.Message}", clientSocket);
        } finally {
            clientSocket.Close();
            this._Log("Client disconnected.");
        }
    }

    private void _StartStateUpdateLoop(Socket client) {
        void sendStateFile(object sender, EventArgs e) {
            try {
                client.Send(Encoding.UTF8.GetBytes("STATE|" + (this._ViewModel.BackupState?.ToJSON(false) ?? "[]") + "\n"));
            } catch (SocketException ex) {
                this._ReportError($"Socket error while sending state: {ex.Message}", client);
                this._ViewModel.JobStateChanged -= sendStateFile;
            } catch (Exception ex) {
                this._ReportError($"Error sending state: {ex.Message}", client);
            }
        }

        this._ViewModel.JobStateChanged += sendStateFile;
    }
}

