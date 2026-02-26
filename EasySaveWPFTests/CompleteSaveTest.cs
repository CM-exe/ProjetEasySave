using ProjetEasySave.Model;

namespace EasySaveWPFTests
{
    /// <summary>
    /// Contains unit tests for the Complete Save strategy.
    /// Ensures that full backup operations correctly copy all files from the source to the destination.
    /// </summary>
    [TestClass]
    public sealed class CompleteSaveTest
    {
        /// <summary>
        /// Verifies that executing a complete save successfully copies a newly created file 
        /// from a temporary source directory to a temporary destination directory.
        /// </summary>
        /// <returns>A task that represents the asynchronous test execution.</returns>
        [TestMethod]
        public async Task verify_complete_save()
        {
            // Given: Create a temporary source directory
            string sourceDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(sourceDirectory);

            // Given: Add a sample file to the source directory
            string sourceFile = Path.Combine(sourceDirectory, "test.txt");
            File.WriteAllText(sourceFile, "This is a test file.");

            // Given: Create a temporary destination directory and required execution parameters
            string destinationDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(destinationDirectory);

            var priorityExt = new List<string>();
            var semaphore = new SemaphoreSlim(1, 1);

            // When: Create a SaveSpace with a CompleteSave strategy and execute the save
            SaveSpace saveSpace = new SaveSpace("TestSaveSpace", sourceDirectory, destinationDirectory, "complete", priorityExt, semaphore);
            await saveSpace.executeSaveAsync();

            // Then: Verify that the file was successfully copied to the destination directory
            string destinationFile = Path.Combine(destinationDirectory, "test.txt");
            Assert.IsTrue(File.Exists(destinationFile), "The file was not copied to the destination directory.");

            // Clean up: Delete the temporary directories to prevent disk clutter
            Directory.Delete(sourceDirectory, true);
            Directory.Delete(destinationDirectory, true);
        }
    }
}