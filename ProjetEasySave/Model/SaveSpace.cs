namespace ProjetEasySave;

public class SaveSpace
{
    public string Name { get; set; }
    public string SourceFolder { get; set; }
    public string DestinationFolder { get; set; }

    public SaveSpace(string name, string sourceFolder, string destinationFolder)
    {
        Name = name;
        SourceFolder = sourceFolder;
        DestinationFolder = destinationFolder;
    }
}