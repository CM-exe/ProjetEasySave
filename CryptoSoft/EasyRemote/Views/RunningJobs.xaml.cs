using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using EasyRemote.Model;

namespace EasyRemote.Views {
    partial class RunningJobs {
        public IViewModel ViewModel { get; private set; }

        public RunningJobs(IViewModel viewModel) {
            ViewModel = viewModel;

            InitializeComponent();
            this.MainGrid.DataContext = viewModel.ClientControler;

            Task.Run(() => {
                while (true) {
                    Application.Current.Dispatcher.Invoke(() => {
                        RunningJobsList.Items.Refresh();
                    });
                    System.Threading.Thread.Sleep(500); // Rafraîchit toutes les secondes
                }
            });

        }

        private void CancelAllButton_Click(object sender, RoutedEventArgs e) {
            foreach (IBackupJob job in ViewModel.ClientControler.BackupJob) {
                ViewModel.ClientControler.CancelProcess(job.Name);
            }
        }

        private void ResumeAllButton_Click(object sender, RoutedEventArgs e) {
            foreach (IBackupJob job in ViewModel.ClientControler.BackupJob) {
                ViewModel.ClientControler.ResumeProcess(job.Name);
            }
        }

        private void PauseAllButton_Click(object sender, RoutedEventArgs e) {
            foreach (IBackupJob job in ViewModel.ClientControler.BackupJob) {
                ViewModel.ClientControler.PauseProcess(job.Name);
            }
        }

        private void PauseOrResumeButton_Click(object sender, RoutedEventArgs e) {
            if (sender is System.Windows.Controls.Button button && button.DataContext is IBackupJobState jobState) {
                if (jobState.State == "PAUSED") {
                    ViewModel.ClientControler.ResumeProcess(jobState.Name);
                } else {
                    ViewModel.ClientControler.PauseProcess(jobState.Name);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            if (sender is System.Windows.Controls.Button button && button.DataContext is IBackupJobState jobState) {
                ViewModel.ClientControler.CancelProcess(jobState.Name);
            }
        }
    }
}