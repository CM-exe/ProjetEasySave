using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using EasyRemote.Model;

namespace EasyRemote.Views
{
    public partial class JobsList : System.Windows.Controls.UserControl
    {
        // Déclaration d'une variable pour stocker l'instance de ViewModel
        public IViewModel ViewModel { get; private set; }

        // Constructeur de la classe JobsList
        public JobsList(IViewModel viewModel)
        {
            this.ViewModel = viewModel; // Correction de la casse pour correspondre au type défini  
            InitializeComponent(); // Déplacer l'initialisation au début pour accéder aux contrôles 

            this.GridMain.DataContext = this;
        }
        private void RunSelectedJobs_Click(object sender, RoutedEventArgs e)
        {
            var selectedJobs = jobsDataGrid.SelectedItems.Cast<IBackupJob>().ToList();
            if (selectedJobs.Any())
            {

                var jobNames = selectedJobs.Select(job => job.Name);
                var jobNamesJoined = string.Join(",", jobNames);
                ViewModel.ClientControler.RunProcess(jobNamesJoined);

            }
        }
    }
}
