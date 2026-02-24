namespace ProjetEasySave.Model
{
    public interface SaveTaskObserver
    {
        /*
         * Observer interface for SaveTask updates.
         */

        // Method to be called when a SaveTask state changes
        SaveTaskState onSaveTaskStateChanged(SaveTask task, SaveTaskState newState);
    }
}
