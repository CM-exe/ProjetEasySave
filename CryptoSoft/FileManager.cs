namespace CryptoSoft;

// Class logic for file encryption
public class FileManager
{

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