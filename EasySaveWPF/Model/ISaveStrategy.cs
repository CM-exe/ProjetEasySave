namespace ProjetEasySave.Model
{
    public interface ISaveStrategy
    {
        /*
         * Interface for save strategies
         */

        // Interface methods
        bool doSave(string sourcePath, string destinationPath);
    }
}