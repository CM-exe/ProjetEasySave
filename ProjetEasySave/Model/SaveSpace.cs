namespace ProjetEasySave;

public class SaveSpace
{
    private readonly string _sourceFolder;
    private readonly string _destinationFolder;
    private readonly string _name;

    public SaveSpace(string sourceFolder, string destinationFolder, string name)
    {
        _sourceFolder = sourceFolder;
        _destinationFolder = destinationFolder;
        _name = name;
    }

    public string getSourceFolder()
    {
        return _sourceFolder;
    }

    public string getDestinationFolder()
    {
        return _destinationFolder;
    }

    public string getName()
    {
        return _name;
    }
}