namespace ProjetEasySave;

class Program
{
    static void Main(string[] args)
    {
        SaveModel model = new SaveModel();
        model.importSaveJobs();

        ViewModel viewModel = new ViewModel(model);
        View view = new View(viewModel);
    }
}