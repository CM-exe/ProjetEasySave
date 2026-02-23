using ProjetEasySave.Utils;
using System.Diagnostics;

namespace ProjetEasySave.Model
{
    public interface ISaveStrategy
    {
        /*
         * Interface for save strategies
         */
        private static Config _config = Config.Instance; // Load config for business software checking
        private static SaveTaskState _state; // The state for the task
        
        // Interface methods
        bool doSave(string sourcePath, string destinationPath, List<string> priorityExt);

        bool isBusinessSoftwareRunning();

        void waitForBusinessSoftwareToClose();

        SaveTaskState setState(SaveTaskState state); // Method to update the state of the SaveTask, can be called by the strategy to notify the SaveTask of a state change
        SaveTaskState getState();
    }
}