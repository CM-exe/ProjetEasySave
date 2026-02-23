using System.Net.Sockets;

public class Server
{
    // Define the ip, the port and the protocol for the server
    private const string IP_ADDRESS = "0.0.0.0"; // Listen on all available network interfaces (for Docker compatibility here)
    private const int PORT = 8080;
    private const ProtocolType PROTOCOL = ProtocolType.Tcp;

    private static List<Thread> _clientThreads = new List<Thread>(); // List to keep track of client threads

    private static readonly string LOG_DIRECTORY_PATH = Path.Combine(Directory.GetCurrentDirectory(), "logs"); // Path to the logs files

    private static Dictionary<string, string> explodeMessage(string message)
    {
        // Explode the message into a dictionary with the format "key:value"
        Dictionary<string, string> explodedMessage = new Dictionary<string, string>();
        // Check if xml format or json format
        if (message != null) {
            if (message.StartsWith("<") && message.EndsWith(">"))
            {
                // XML format
                try
                {
                    System.Xml.Linq.XDocument xmlDoc = System.Xml.Linq.XDocument.Parse(message);
                    foreach (var element in xmlDoc.Descendants())
                    {
                        if (!element.HasElements) // Only add leaf nodes to the dictionary
                        {
                            explodedMessage[element.Name.LocalName] = element.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error parsing XML message: " + ex.Message);
                }
            }
            else if ((message.StartsWith("{") && message.EndsWith("}")) || (message.StartsWith("[") && message.EndsWith("]")))
            {
                // JSON format
                try
                {
                    var jsonObject = System.Text.Json.JsonDocument.Parse(message).RootElement;
                    foreach (var property in jsonObject.EnumerateObject())
                    {
                        explodedMessage[property.Name] = property.Value.ToString();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error parsing JSON message: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Unknown message format: " + message);
            }
        }
        // Add the format into the dictionary
        if (message != null)
        {
            if (message.StartsWith("<") && message.EndsWith(">"))
            {
                explodedMessage["format"] = "xml";
            }
            else if ((message.StartsWith("{") && message.EndsWith("}")) || (message.StartsWith("[") && message.EndsWith("]")))
            {
                explodedMessage["format"] = "json";
            }
            else
            {
                explodedMessage["format"] = "unknown";
            }
        }
        return explodedMessage;
    }

    private static void log(string message)
    {
        // Create the log file if it doesn't exist and write the message to the log file
        Console.WriteLine(message);
        Console.WriteLine(explodeMessage(message));
        // Create a directory if doesn't exist for the user recevied in the message
        string userDirectoryPath = Path.Combine(LOG_DIRECTORY_PATH, explodeMessage(message).GetValueOrDefault("user", "unknown_user"));
        string userDirectory = Path.Combine(userDirectoryPath, DateTime.Now.ToString("yyyy-MM-dd"));
        Console.WriteLine(userDirectory);
        if (!Directory.Exists(userDirectory))
        {
            Directory.CreateDirectory(userDirectory);
        }
        // Create a log file for the current day if it doesn't exist and append the message to the log file
        string logFilePath = Path.Combine(userDirectory, "log.txt");
        if (!File.Exists(logFilePath))
        {
            File.Create(logFilePath).Close();
        }
        // Depending on the format, create an array with json logs or a string with xml logs and write it to the log file
        if (explodeMessage(message).GetValueOrDefault("format") == "json")
        {
            // Create an array with json logs
            List<string> jsonLogs = new List<string>();
            if (File.Exists(logFilePath))
            {
                string existingLogs = File.ReadAllText(logFilePath);
                if (!string.IsNullOrEmpty(existingLogs))
                {
                    try
                    {
                        var existingJsonLogs = System.Text.Json.JsonDocument.Parse(existingLogs).RootElement;
                        if (existingJsonLogs.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var log in existingJsonLogs.EnumerateArray())
                            {
                                jsonLogs.Add(log.GetRawText());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error parsing existing JSON logs: " + ex.Message);
                    }
                }
            }
            jsonLogs.Add(message);
            File.WriteAllText(logFilePath, "[" + string.Join(",", jsonLogs) + "]");
        }
        else if (explodeMessage(message).GetValueOrDefault("format") == "xml")
        {
            // Create a string with xml logs
            string existingLogs = File.ReadAllText(logFilePath);
            string newLogEntry = message;
            if (!string.IsNullOrEmpty(existingLogs))
            {
                existingLogs = existingLogs.Trim();
                if (existingLogs.StartsWith("<logs>") && existingLogs.EndsWith("</logs>"))
                {
                    existingLogs = existingLogs.Substring(5, existingLogs.Length - 11); // Remove the <logs> root element
                    newLogEntry = "<logs>" + existingLogs + newLogEntry + "</logs>";
                }
                else
                {
                    newLogEntry = "<logs>" + newLogEntry + "</logs>";
                }
            }
            else
            {
                newLogEntry = "<logs>" + newLogEntry + "</logs>";
            }
            File.WriteAllText(logFilePath, newLogEntry);
        }
    }

    private static Socket startServer()
    {
        // Create a new socket for the server
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, PROTOCOL);
        // Bind the socket to the specified IP address and port
        serverSocket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Parse(IP_ADDRESS), PORT));
        Console.WriteLine("Server started and bound to IP: " + IP_ADDRESS + ", Port: " + PORT);
        return serverSocket;
    }

    private static (string, int) getClientInfo(Socket clientSocket)
    {
        // Get the client's IP address and port number
        string ipClient = ((System.Net.IPEndPoint)clientSocket.RemoteEndPoint).Address.ToString();
        int portClient = ((System.Net.IPEndPoint)clientSocket.RemoteEndPoint).Port;
        return (ipClient, portClient);
    }

    public static Socket acceptConnection(Socket serverSocket)
    {
        // Start listening for incoming connections
        serverSocket.Listen(10); // 10 is the backlog size, which is the maximum number of pending connections
        // Accept an incoming connection
        Socket clientSocket = serverSocket.Accept();
        (string ipClient, int portClient) = getClientInfo(clientSocket);
        Console.WriteLine($"Client connected from IP: {ipClient}, Port: {portClient}");
        return clientSocket;
    }

    private static void listenToClients(Socket clientSocket, int id)
    {
        // Listen for incoming data from the client and return it in uppercase
        byte[] buffer = new byte[1024]; // Buffer to store incoming data set to 1024 bytes length
        (string ipClient, int portClient) = getClientInfo(clientSocket);
        while (true)
        {
            int bytesRead = clientSocket.Receive(buffer); // Receive data from the client and store it in the buffer, returns the number of bytes read
            if (bytesRead > 0) // If data was received, process it
            {
                // Convert the received bytes to a string and print it to the console
                string receivedData = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received from client " + ipClient + ":" + portClient.ToString() + " (client n°" + id.ToString() + "): " + receivedData);
                // Log the received data to the log file
                log(receivedData);
                // Send a response back to the client (for demonstration purposes, we just send back the log directory path concatenated with the received data)
                Console.WriteLine("Sending response to client " + ipClient + ":" + portClient.ToString() + " (client n°" + id.ToString() + "): " + LOG_DIRECTORY_PATH + " - " + receivedData);
            }
            else // If no data was received, the client has disconnected
            {
                Console.WriteLine("Client " + ipClient + ":" + portClient.ToString() + " (client n°" + id.ToString() + ") disconnected.");
                break;
            }
        }
    }

    private static void disconnet(Socket socketClient, int id)
    {
        // Close the client socket and log the disconnection
        socketClient.Close();
        Console.WriteLine("Client " + id.ToString() + " socket closed.");
    }

    public static void Main(string[] args)
    {
        // Start the server and accept connections and listen for messages from clients
        Socket serverSocket = startServer();
        Console.WriteLine("Server is listening for incoming connections...");
        while (true)
        {
            // Process
            Socket clientSocket = acceptConnection(serverSocket);
            Thread clientThread = new Thread(() =>
            {
                listenToClients(clientSocket, _clientThreads.Count + 1);
                disconnet(clientSocket, _clientThreads.Count + 1);
            });
            clientThread.Start();
            _clientThreads.Add(clientThread);
        }
        // Clean up: Close the server socket and wait for all client threads to finish
        // serverSocket.Close();
        // foreach (Thread clientThread in _clientThreads)
        // {
        //     clientThread.Join();
        // }
    }
}