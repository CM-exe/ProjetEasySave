namespace CryptoSoft;

// Class logic for file encryption
public class FileManager
{
    private static readonly System.Threading.Mutex _mutex;
    private static readonly bool _isSingleInstance;

    static FileManager()
    {
        bool createdNew;
        _mutex = new System.Threading.Mutex(true, "Global\\CryptoSoft_FileManager_Mutex", out createdNew);
        _isSingleInstance = createdNew;
    }

    // Indicates whether this process is the only instance (true) or another instance already holds the mutex (false)
    public static bool IsSingleInstance => _isSingleInstance;

    // If this process created and owns the mutex, release it (optional; mutex will be released on process exit)
    public static void ReleaseInstance()
    {
        if (_isSingleInstance)
        {
            try { _mutex.ReleaseMutex(); }
            catch { }
        }
    }

    // Wait for acquire mutex method
    public static void WaitForInstance()
    {
        if (!_isSingleInstance)
        {
            _mutex.WaitOne();
        }
    }

    // Static method to encrypt a source file to a destination path
    public static double CryptFile(string sourcePath, string destPath, string key)
    {
        if (!File.Exists(sourcePath)) return -1;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Read source, encrypt data, write to destination
        var fileBytes = File.ReadAllBytes(sourcePath);
        var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);

        // XOR encryption process
        var result = new byte[fileBytes.Length];
        for (var i = 0; i < fileBytes.Length; i++)
        {
            result[i] = (byte)(fileBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }

        File.WriteAllBytes(destPath, result);
        stopwatch.Stop();

        return stopwatch.ElapsedMilliseconds;
    }
}

/* Usage exemple:
if (FileManager.IsSingleInstance)
{
    // This is the only instance, proceed with encryption
    double timeTaken = FileManager.CryptFile("path/to/source.txt", "path/to/dest.txt", "encryptionKey");
    Console.WriteLine($"File encrypted in {timeTaken} ms");
}
else
{
    // Another instance is already running, wait for it to finish
    Console.WriteLine("Another instance is running. Waiting...");
    FileManager.WaitForInstance();
    Console.WriteLine("Previous instance finished. You can now run the encryption.");
}
*/