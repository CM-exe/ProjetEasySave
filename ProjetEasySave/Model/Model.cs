using static System.String;

namespace ProjetEasySave;
using System.IO;
using System.Text.Json;

public class Model
{
    private List<SaveSpace> _saveSpaces = new List<SaveSpace>();
    private const string FilePath = "../../../Config/config.json"; // TODO - To change !! Imperative !

    public void addSaveSpace(SaveSpace saveSpace)
    {
        // Can be up later 
        if (_saveSpaces.Count < 5)
        {
            _saveSpaces.Add(saveSpace);
        }
        else
        {
            // TODO return an user sided error 
        }
    }

    public void importSaveSpaces()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                InitializeDefaultSlots();
                return;
            }
            string jsonString = File.ReadAllText(FilePath);
            if (IsNullOrWhiteSpace(jsonString))
            {
                InitializeDefaultSlots();
                return;
            }
            var imported = JsonSerializer.Deserialize<List<SaveSpace>>(jsonString);
            _saveSpaces = imported ?? new List<SaveSpace>();
        }
        catch (JsonException ex)
        {
            // TODO return an user sided error
            InitializeDefaultSlots();
        }
        catch (Exception ex)
        {
            // TODO return an user sided error
            InitializeDefaultSlots();
        }
    }

    private void InitializeDefaultSlots()
    {
        _saveSpaces = new List<SaveSpace>();
    }

    public void exportSaveSpaces()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(_saveSpaces, options);

        Directory.CreateDirectory(Path.GetDirectoryName(FilePath) ?? Empty);
        
        File.WriteAllText(FilePath, jsonString);
    }
}