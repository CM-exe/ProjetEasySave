using ProjetEasySave.Model;

namespace EasySaveWPFTests
{
    /// <summary>
    /// Contains unit tests for the Differential Save strategy.
    /// Ensures that differential backup operations correctly detect modified files and copy only the changes compared to a reference full backup.
    /// </summary>
    [TestClass]
    public sealed class DifferentialSaveTest
    {
        /// <summary>
        /// Verifies that a differential save correctly detects a modified file in the source directory 
        /// and successfully copies the updated version to the differential destination directory.
        /// </summary>
        /// <returns>A task that represents the asynchronous test execution.</returns>
        [TestMethod]
        public async Task verify_differential_save()
        {
            // Given: Initialize temporary directories and files for testing
            string sourceDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(sourceDirectory);

            string sourceFile = Path.Combine(sourceDirectory, "test.txt");
            File.WriteAllText(sourceFile, "Initial content.");

            string destinationDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(destinationDirectory);

            string differentialDestinationDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(differentialDestinationDirectory);

            var priorityExt = new List<string>();
            var semaphore = new SemaphoreSlim(1, 1);

            // First save (complete): Establish the baseline for the differential save
            SaveSpace completeSaveSpace = new SaveSpace("TestSaveSpace", sourceDirectory, destinationDirectory, "complete", priorityExt, semaphore);
            await completeSaveSpace.executeSaveAsync();

            // Get file info after the first complete save to use for comparison
            string destinationFile = Path.Combine(destinationDirectory, "test.txt");
            Assert.IsTrue(File.Exists(destinationFile), "The file was not copied to the destination directory.");
            long initialSize = new FileInfo(destinationFile).Length;

            // Modify the file in the source directory to trigger the differential condition
            File.WriteAllText(sourceFile, "Modified content.");

            // When: Execute the second save (differential) using the complete save as a reference
            SaveSpace differentialSaveSpace = new SaveSpace("TestSaveSpace", sourceDirectory, differentialDestinationDirectory, "differential", priorityExt, semaphore, destinationDirectory);
            await differentialSaveSpace.executeSaveAsync();

            // Then: Verify the modified file was copied to the differential destination
            string diffFile = Path.Combine(differentialDestinationDirectory, "test.txt");
            Assert.IsTrue(File.Exists(diffFile), "The file was not copied to the differential destination directory.");
            long diffSize = new FileInfo(diffFile).Length;

            // The size should be different since the file content was modified
            Assert.AreNotEqual(initialSize, diffSize, "The file size did not change after modification and differential save.");

            // Clean up: Remove temporary directories to free up disk space
            Directory.Delete(sourceDirectory, true);
            Directory.Delete(destinationDirectory, true);
            Directory.Delete(differentialDestinationDirectory, true);
        }
    }
}