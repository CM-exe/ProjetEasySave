namespace ProjetEasySave;

class Program
{
    static void Main(string[] args)
    {
        Model model = new Model();
        model.importSaveJobs();

        ViewModel viewModel = new ViewModel(model);
        View view = new View(viewModel);
    }
}