using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EasyRemote.Views;

namespace EasyRemote
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public IViewModel ViewModel { get; private set; }

        // Constructor that takes an IViewModel instance as a parameter
        public MainWindow(IViewModel viewModel)
        {
            // get the ViewModel instance from the parameter
            this.ViewModel = viewModel;

            // Initialize the ViewModel instance
            InitializeComponent();

            // Set the DataContext of the MainWindow to the ViewModel instance
            MainContent.Content = new JobsList(viewModel);

            this.DataContext = this;
        }
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            ViewModel.ClientControler.DisconnectToServer(null);
        }
        private void JobsList_Click(object sender, RoutedEventArgs e)
        {
            // Set the content of the MainContent to the JobsList view
            MainContent.Content = new JobsList(ViewModel); // Fixed: Use the instance field _ViewModel instead of the type ViewModel
        }
        // Event handler for the "Running Jobs" menu item click
        private void RunningJobs_Click(object sender, RoutedEventArgs e)
        {
            // Set the content of the MainContent to the RunningJobs view
            MainContent.Content = new RunningJobs(this.ViewModel);
        }

    }
}