namespace CryptoSoft;

/// <summary>
/// Provides logic for file encryption and decryption using XOR operations.
/// Ensures single-instance execution across the system using a global Mutex.
/// </summary>
/// <example>
/// Usage example:
/// <code>
/// if (FileManager.IsSingleInstance)
/// {
///     // This is the only instance, proceed with encryption
///     double timeTaken = FileManager.CryptFile("path/to/source.txt", "path/to/dest.txt", "encryptionKey");
///     Console.WriteLine($"File encrypted in {timeTaken} ms");
/// }
/// else
/// {
///     // Another instance is already running, wait for it to finish
///     Console.WriteLine("Another instance is running. Waiting...");
///     FileManager.WaitForInstance();
///     Console.WriteLine("Previous instance finished. You can now run the encryption.");
/// }
/// </code>
/// </example>
public class FileManager
{
    private static readonly System.Threading.Mutex _mutex;
    private static readonly bool _isSingleInstance;

    /// <summary>
    /// Static constructor to initialize the global Mutex. 
    /// Determines if the current process is the first one to request the encryption lock.
    /// </summary>
    static FileManager()
    {
        bool createdNew;
        _mutex = new System.Threading.Mutex(true, "Global\\CryptoSoft_FileManager_Mutex", out createdNew);
        _isSingleInstance = createdNew;
    }

    /// <summary>
    /// Gets a value indicating whether this process is the only instance (true), 
    /// or if another instance already holds the encryption mutex (false).
    /// </summary>
    public static bool IsSingleInstance => _isSingleInstance;

    /// <summary>
    /// Releases the global mutex if this process created and owns it.
    /// Note: This is optional as the mutex will be automatically released upon process exit.
    /// </summary>
    public static void ReleaseInstance()
    {
        if (_isSingleInstance)
        {
            try { _mutex.ReleaseMutex(); }
            catch { }
        }
    }

    /// <summary>
    /// Blocks the current thread and waits until the global mutex is released by another running instance.
    /// </summary>
    public static void WaitForInstance()
    {
        if (!_isSingleInstance)
        {
            _mutex.WaitOne();
        }
    }

    /// <summary>
    /// Encrypts (or decrypts) a source file and writes the result to a destination path using an XOR cipher.
    /// </summary>
    /// <param name="sourcePath">The absolute or relative path to the file to be encrypted.</param>
    /// <param name="destPath">The absolute or relative path where the encrypted file will be saved.</param>
    /// <param name="key">The encryption key used for the XOR operation.</param>
    /// <returns>The time taken to complete the encryption process in milliseconds. Returns <c>-1</c> if the source file does not exist.</returns>
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