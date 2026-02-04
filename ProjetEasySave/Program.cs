namespace ProjetEasySave;

class Program
{
    static void Main(string[] args)
    {
        Model model = new Model();

        // ViewModel viewModel = new ViewModel(model);
        // View view = new View(viewModel);

        
        SaveSpace s1 = new SaveSpace("Projet1", "~/Documents/ProjetES/Source", "~/Documents/ProjetES/Save");

        
        model.importSaveSpaces();
        
        model.addSaveSpace(s1);
        
        model.exportSaveSpaces();
        
        

    }
}