using ProjetEasySave.Utils;

namespace ProjetEasySave.Model
{
    /// <summary>
    /// Defines the contract for backup strategies within the EasySave application.
    /// Implementations of this interface (such as Complete or Differential saves) must provide specific logic for copying files, handling states, and reacting to business software constraints.
    /// </summary>
    public interface ISaveStrategy
    {
        private static Config _config = Config.Instance; // Load config for business software checking
        private static SaveTaskState _state; // The state for the task

        /// <summary>
        /// Executes the core backup operation according to the specific rules of the implemented strategy.
        /// </summary>
        /// <param name="sourcePath">The source directory path that contains the files to be backed up.</param>
        /// <param name="destinationPath">The destination directory path where the backup will be stored.</param>
        /// <param name="priorityExt">A list of file extensions dictating which files should be transferred first.</param>
        /// <param name="token">A cancellation token used to safely abort the backup process.</param>
        /// <param name="pauseEvent">A manual reset event used to block or unblock the thread for pause/resume functionality.</param>
        /// <param name="progress">An action delegate to report the overall completion percentage and the name of the file currently being processed.</param>
        /// <returns><c>true</c> if the backup completes successfully; otherwise, <c>false</c>.</returns>
        public bool doSave(string sourcePath, string destinationPath, List<string> priorityExt, CancellationToken token, ManualResetEventSlim pauseEvent, Action<int, string> progress);

        /// <summary>
        /// Checks whether the user-configured business software is actively running on the host system.
        /// </summary>
        /// <returns><c>true</c> if an instance of the business software is running; otherwise, <c>false</c>.</returns>
        bool isBusinessSoftwareRunning();

        /// <summary>
        /// Blocks the execution of the backup process and waits until all instances of the conflicting business software have been closed by the user.
        /// </summary>
        void waitForBusinessSoftwareToClose();

        /// <summary>
        /// Updates the current execution state of the backup task (e.g., RUNNING, PAUSED, COMPLETED) and notifies the parent task.
        /// </summary>
        /// <param name="state">The new <see cref="SaveTaskState"/> to apply.</param>
        /// <returns>The newly applied state.</returns>
        SaveTaskState setState(SaveTaskState state); // Method to update the state of the SaveTask, can be called by the strategy to notify the SaveTask of a state change

        /// <summary>
        /// Retrieves the current execution state of the backup strategy.
        /// </summary>
        /// <returns>The current <see cref="SaveTaskState"/>.</returns>
        SaveTaskState getState();
    }
}