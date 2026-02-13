using System.Windows;
using EasySave.Views;

namespace EasySave;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    // Declare a variable to store the ViewModel instance
    public IViewModel ViewModel { get; private set; }

    // Constructor that takes an IViewModel instance as a parameter
    public MainWindow(IViewModel viewModel) {
        // get the ViewModel instance from the parameter
        this.ViewModel = viewModel;

        // Initialize the ViewModel instance
        InitializeComponent();

        // Set the DataContext of the MainWindow to the ViewModel instance
        MainContent.Content = new JobsList(viewModel);

        this.DataContext = this;
    }

    // Event handlers for menu item clicks
    private void JobsList_Click(object sender, RoutedEventArgs e) {
        // Set the content of the MainContent to the JobsList view
        MainContent.Content = new JobsList(ViewModel); // Fixed: Use the instance field _ViewModel instead of the type ViewModel
    }
    // Event handler for the "Running Jobs" menu item click
    private void RunningJobs_Click(object sender, RoutedEventArgs e) {
        // Set the content of the MainContent to the RunningJobs view
        MainContent.Content = new RunningJobs(this.ViewModel);
    }
    // Event handler for the "Logs" menu item click
    private void Logs_Click(object sender, RoutedEventArgs e) {
        // Set the content of the MainContent to the Logs view
        MainContent.Content = new Logs(ViewModel);
    }
    // Event handler for the "Configuration" menu item click
    private void Configuration_Click(object sender, RoutedEventArgs e) {
        // Set the content of the MainContent to the Configuration view
        MainContent.Content = new Configuration(ViewModel);
    }
}
