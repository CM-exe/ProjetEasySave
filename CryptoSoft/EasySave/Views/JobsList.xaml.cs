using System.Windows.Controls;
using EasySave.Views;
using EasySave.Model;
using System.Windows;
using EasySave.Helpers;


namespace EasySave.Views {
    public partial class JobsList : System.Windows.Controls.UserControl {
        // Déclaration d'une variable pour stocker l'instance de ViewModel
        public IViewModel ViewModel { get; private set; }

        // Constructeur de la classe JobsList
        public JobsList(IViewModel viewModel) {
            this.ViewModel = viewModel; // Correction de la casse pour correspondre au type défini  
            InitializeComponent(); // Déplacer l'initialisation au début pour accéder aux contrôles 

            this.GridMain.DataContext = this;
        }

        private void AddJob(object sender, System.Windows.RoutedEventArgs e) {
            if (sender is System.Windows.Controls.Button button) {
                var selectedJob = new BackupJobConfiguration();
                JobEdit jobEdit = new(ViewModel, selectedJob);
                if (jobEdit.ShowDialog())
                {

                    this.ViewModel.Configuration.AddJob(selectedJob);
                }
            }
        }

        private void EditJob_Click(object sender, System.Windows.RoutedEventArgs e) {
            if (sender is System.Windows.Controls.Button button) {
                var selectedJob = (IBackupJobConfiguration)button.DataContext;
                JobEdit jobEdit = new(ViewModel, selectedJob);
                jobEdit.Show();
            }
        }

        private void DeleteJob_Click(object sender, System.Windows.RoutedEventArgs e) {
            if (sender is System.Windows.Controls.Button button) {
                // Show confirmation dialog  
                ConfirmDeleteWindow confirmDeleteWindow = new();
                // Show the confirmation dialog and wait for user response
                if (confirmDeleteWindow.ShowDialog() == true && confirmDeleteWindow.IsConfirmed) {
                    Model.Configuration.Instance?.RemoveJob((IBackupJobConfiguration)button.DataContext);
                    jobsDataGrid.Items.Refresh();
                }
            }
        }

        // Démarrer le job sélectionné
        private void RunSelectedJobs_Click(object sender, System.Windows.RoutedEventArgs e) {
            List<IBackupJobConfiguration> selectedJobs = [];
            foreach (var item in jobsDataGrid.SelectedItems) {
                if (item is IBackupJobConfiguration job) {
                    selectedJobs.Add(job);
                }
            }

            if (selectedJobs.Count == 0) return;
            this.ViewModel.Commands.RunCommand("run", [..selectedJobs.Select(job => job.Name)]);
        }
    }
}
