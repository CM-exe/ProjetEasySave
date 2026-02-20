using ProjetEasySave.Model;
using System.IO;
using System.Threading.Tasks;

namespace EasySaveWPFTests
{
    [TestClass]
    public sealed class DifferentialSaveTest
    {
        [TestMethod]
        public async Task verify_differential_save()
        {
            // Given
            string sourceDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(sourceDirectory);
            string sourceFile = Path.Combine(sourceDirectory, "test.txt");
            File.WriteAllText(sourceFile, "Initial content.");
            string destinationDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(destinationDirectory);
            string differentialDestinationDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(differentialDestinationDirectory);

            // First save (complete)
            SaveSpace completeSaveSpace = new SaveSpace("TestSaveSpace", sourceDirectory, destinationDirectory, "complete");
            await completeSaveSpace.executeSaveAsync();

            // Get file info after first save
            string destinationFile = Path.Combine(destinationDirectory, "test.txt");
            Assert.IsTrue(File.Exists(destinationFile), "The file was not copied to the destination directory.");
            long initialSize = new FileInfo(destinationFile).Length;

            // Modify the file in the source directory
            File.WriteAllText(sourceFile, "Modified content.");

            // When
            // Second save (differential)
            SaveSpace differentialSaveSpace = new SaveSpace("TestSaveSpace", sourceDirectory, differentialDestinationDirectory, "differential", destinationDirectory);
            await differentialSaveSpace.executeSaveAsync();

            // Then
            string diffFile = Path.Combine(differentialDestinationDirectory, "test.txt");
            Assert.IsTrue(File.Exists(diffFile), "The file was not copied to the differential destination directory.");
            long diffSize = new FileInfo(diffFile).Length;

            // The size should be different if the file was modified and encrypted/copied again
            Assert.AreNotEqual(initialSize, diffSize, "The file size did not change after modification and differential save.");

            // Clean up
            Directory.Delete(sourceDirectory, true);
            Directory.Delete(destinationDirectory, true);
            Directory.Delete(differentialDestinationDirectory, true);
        }
    }
}
