using ProjetEasySave.Model;

namespace EasySaveWPFTests
{
    [TestClass]
    public sealed class CompleteSaveTest
    {
        [TestMethod]
        public async Task verify_complete_save()
        {
            // Given
            // Create a source directory
            string sourceDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()).ToString();
            Directory.CreateDirectory(sourceDirectory);
            // Add a file to the source directory
            string sourceFile = Path.Combine(sourceDirectory, "test.txt");
            File.WriteAllText(sourceFile, "This is a test file.");
            // Create a destination directory
            string destinationDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()).ToString();
            Directory.CreateDirectory(destinationDirectory);
            var priorityExt = new List<string>();
            var semaphore = new SemaphoreSlim(1, 1);

            // When
            // Create a SaveSpace with a CompleteSave strategy and execute the save
            SaveSpace saveSpace = new SaveSpace("TestSaveSpace", sourceDirectory, destinationDirectory, "complete", priorityExt, semaphore);
            await saveSpace.ExecuteAsync();

            // Then
            // Verify that the file was copied to the destination directory
            string destinationFile = Path.Combine(destinationDirectory, "test.txt");
            Assert.IsTrue(File.Exists(destinationFile), "The file was not copied to the destination directory.");
            // Clean up
            Directory.Delete(sourceDirectory, true);
            Directory.Delete(destinationDirectory, true);
        }
    }
}
