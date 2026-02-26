namespace ProjetEasySave.Model
{
    /// <summary>
    /// Defines an observer interface for monitoring state changes within a backup task.
    /// Implementing classes (such as <see cref="SaveSpace"/>) can use this to track and react to the lifecycle events of a <see cref="SaveTask"/>.
    /// </summary>
    public interface SaveTaskObserver
    {
        /// <summary>
        /// Invoked when the state of an observed <see cref="SaveTask"/> changes.
        /// </summary>
        /// <param name="task">The specific <see cref="SaveTask"/> instance that triggered the state change.</param>
        /// <param name="newState">The new <see cref="SaveTaskState"/> applied to the task.</param>
        /// <returns>The confirmed <see cref="SaveTaskState"/> after processing the change.</returns>
        SaveTaskState onSaveTaskStateChanged(SaveTask task, SaveTaskState newState);
    }
}
