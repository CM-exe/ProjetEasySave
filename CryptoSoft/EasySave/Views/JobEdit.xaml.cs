using System;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Forms;
using EasySave.Model; // Ajout de cette directive

namespace EasySave.Views {
    public partial class JobEdit : Window {
        // Déclaration d'une variable pour stocker le DataContext de la vue modèle
        public IViewModel ViewModel { get; private set; }
        public IBackupJobConfiguration Job { get; set; } // Propriété pour stocker la configuration du job de sauvegarde
        // Déclaration d'une variable pour stocker le DataContext de la vue modèle
        public bool IsOk { get; set; }

        public new bool ShowDialog() { 
            base.ShowDialog();
            return IsOk;
        }
        public JobEdit(IViewModel viewModel, IBackupJobConfiguration job) {
            ViewModel = viewModel; // Lier le DataContext à la vue modèle  
            this.Job = job; // Initialiser la propriété Job avec la configuration du job de sauvegarde
            // Initialiser le DataContext de la fenêtre
            InitializeComponent();

            this.DataContext = this;
        }

        // Update the Save_Click method to pass the required 'propertyName' argument to OnPropertyChanged.
        private void Save_Click(object sender, RoutedEventArgs e) {
            try {
                // Validate inputs  
                if (string.IsNullOrWhiteSpace(PathSource.Text) || string.IsNullOrWhiteSpace(PathDestination.Text)) {
                    // Show error message if any field is empty
                    System.Windows.MessageBox.Show("Veuillez remplir tous les champs.");
                    return;
                }

                // Cast DataContext to the appropriate ViewModel type  

                //change the property
                ViewModel.OnPropertyChanged(nameof(ViewModel.BackupJobs));

                this.IsOk = true;
                //close the window
                this.Close();
            } catch (Exception ex) {
                System.Windows.MessageBox.Show("Erreur lors de la sauvegarde : " + ex.Message);
            }
        }

        // Event handler for the Cancel button click  
        private void Cancel_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void DeleteJob(object sender, RoutedEventArgs e) {
            // ask for confirmation before deleting the job
            try {
                // Show confirmation dialog  
                ConfirmDeleteWindow confirmDeleteWindow = new();
                // Show the confirmation dialog and wait for user response
                if (confirmDeleteWindow.ShowDialog() == true && confirmDeleteWindow.IsConfirmed) {
                    this.ViewModel.Configuration.RemoveJob(this.Job);

                    System.Windows.MessageBox.Show("Suppression réussie !");
                    this.Close();

                }
            }
            // Handle any exceptions that may occur during the deletion process
            catch (Exception ex) {
                // Show error message if an exception occurs
                System.Windows.MessageBox.Show("Erreur lors de la suppression : " + ex.Message);
            }
        }

        // Event handler for the Browse Source button click  
        private void BrowseSource_Click(object sender, RoutedEventArgs e) {
            // Open a folder browser dialog to select the source path
            try {
                // Create and configure the folder browser dialog
                using (var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog {
                    // Set the description for the dialog
                    Description = "Sélectionnez un dossier source",
                    // Show the button to create a new folder
                    ShowNewFolderButton = true
                }) {
                    // Show the dialog and check if the user clicked OK
                    if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                        // Set the selected path to the PathSource TextBox
                        PathSource.Text = folderBrowserDialog.SelectedPath;
                        Job.Source = folderBrowserDialog.SelectedPath;
                    }
                }
            }
            // Handle any exceptions that may occur during the folder selection process
            catch (Exception ex) {
                // Show error message if an exception occurs
                System.Windows.MessageBox.Show("Erreur lors de la sélection du dossier source : " + ex.Message);
            }
        }

        // Event handler for the Browse Destination button click  
        private void BrowseDestination_Click(object sender, RoutedEventArgs e) {
            // Open a folder browser dialog to select the destination path
            try {
                // Create and configure the folder browser dialog
                using (var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog {
                    // Set the description for the dialog
                    Description = "Sélectionnez un dossier de destination",
                    // Show the button to create a new folder
                    ShowNewFolderButton = true
                }) {
                    // Show the dialog and check if the user clicked OK
                    if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                        // Set the selected path to the PathDestination TextBox
                        PathDestination.Text = folderBrowserDialog.SelectedPath;
                        Job.Destination = folderBrowserDialog.SelectedPath;
                    }
                }
            }
            // Handle any exceptions that may occur during the folder selection process
            catch (Exception ex) {
                // Show error message if an exception occurs
                System.Windows.MessageBox.Show("Erreur lors de la sélection du dossier de destination : " + ex.Message);
            }
        }
    }
}