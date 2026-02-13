using EasySave.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Controls;
using System.ComponentModel;

namespace EasySave.Views {
    /// <summary>
    /// Represents the RunningJobs view, which displays the currently running backup jobs.
    /// </summary>
    partial class RunningJobs : INotifyPropertyChanged {
        /// <summary>
        /// ViewModel associated with this view.
        /// </summary>
        public IViewModel ViewModel { get; private set; }
        private readonly ObservableCollection<IBackupJobState> _RunningJobList = [];
        public IEnumerable<IBackupJobState> RunningJobList => _RunningJobList;
        private readonly Task _RefreshTask;

        public DateTime? StartedAt => _RunningJobList.Count == 0 ? null : _RunningJobList.Min(job => job.BackupJob.StartedAt);
        public int TotalFilesToCopy => _RunningJobList.Count == 0 ? 0 : (int)_RunningJobList.Sum(job => job.TotalFilesToCopy);
        public int TotalFilesLeft => _RunningJobList.Count == 0 ? 0 : (int)_RunningJobList.Sum(job => job.FilesLeft);

        // calculates the overall progression of all running jobs
        public double Progression {
            get {
                if (_RunningJobList.Count == 0) return 0.0;

                int totalFiles = TotalFilesToCopy;
                int filesLeft = TotalFilesLeft;

                if (totalFiles == 0) return 0.0;

                double completedFiles = totalFiles - filesLeft;
                double progressPercentage = (completedFiles / totalFiles) * 100.0;

                // S'assurer que la valeur est entre 0 et 100
                return Math.Max(0.0, Math.Min(100.0, progressPercentage));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RunningJobs"/> class.
        /// </summary>
        /// <param name="viewModel"></param>
        public RunningJobs(IViewModel viewModel) {
            this.ViewModel = viewModel;
            InitializeComponent();
            this.MainGrid.DataContext = this;
            this.ViewModel.JobStateChanged += this.OnJobStateChanged;

            this._RefreshTask = Task.Run(() => {
                while (true) {
                    this.Dispatcher.Invoke(() => {
                        this.RunningJobsList.Items.Refresh();
                        this.UpdateStats();
                    });
                    Task.Delay(1000).Wait();
                }
            });

            this.Dispatcher.Invoke(() => {
                this.UpdateList();
            });
            this.UpdateStats();
        }

        /// <summary>
        /// Updates the list of running jobs based on the current backup state.
        /// </summary>

        private void UpdateList() {
            this._RunningJobList.Clear();
            if (this.ViewModel.BackupState is null) {
                return;
            }

            List<IBackupJobState> jobsState = [.. this.ViewModel.BackupState.JobState];
            foreach (IBackupJobState jobState in jobsState) {
                if (jobState.State == State.ACTIVE || jobState.State == State.IN_PROGRESS || jobState.State == State.PAUSED) {
                    this._RunningJobList.Add(jobState);
                }
            }
            this.RunningJobsList.Items.Refresh();
        }
        /// <summary>
        /// Updates the statistics displayed in the view, such as the start time, total files to copy, total files left, and overall progression.
        /// </summary>

        private void UpdateStats() {
            this.OnPropertyChanged(nameof(StartedAt));
            this.OnPropertyChanged(nameof(TotalFilesToCopy));
            this.OnPropertyChanged(nameof(TotalFilesLeft));
            this.OnPropertyChanged(nameof(Progression));
        }

        /// <summary>
        /// Handles the JobStateChanged event, which is triggered when the state of a backup job changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnJobStateChanged(object sender, JobStateChangedEventArgs e) {
            this.Dispatcher.Invoke(() => {
                if (e.JobState is not null) {
                    this.UpdateList();
                }
                this.UpdateStats();
            });
        }
        /// <summary>
        /// Handles the CancelAllButton click event, which cancels all running backup jobs.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelAllButton_Click(object sender, RoutedEventArgs e) {
            foreach (IBackupJob job in ViewModel.BackupJobs) {
                job.Stop();
            }
        }

        /// <summary>
        /// Handles the RunAllButton click event, which runs all backup jobs defined in the configuration.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunAllButton_Click(object sender, RoutedEventArgs e) {
            this.ViewModel.Commands.RunCommand("run", [.. this.ViewModel.Configuration.Jobs.Select(j => j.Name)]);
        }

        /// <summary>
        /// Handles the CancelButton click event, which cancels a specific backup job.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            if (sender is System.Windows.Controls.Button button && button.DataContext is IBackupJobState jobState) {
                jobState.BackupJob.Stop();
            }
        }

        /// <summary>
        /// Notifies subscribers when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property name.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void PauseOrResumeButton_Click(object sender, RoutedEventArgs e) {
            if (sender is System.Windows.Controls.Button button && button.DataContext is IBackupJobState jobState) {
                if (jobState.BackupJob.IsPaused) {
                    jobState.BackupJob.Resume();
                } else {
                    jobState.BackupJob.Pause();
                }
            }
        }
    }
}

/// <summary>
/// Extension methods for the IBackupJobState interface to calculate progression percentage.
/// </summary>
public static class BackupJobStateExtensions {
    /// <summary>
    /// Calculates the progression percentage of a backup job based on the total files to copy and the files left to copy.
    /// </summary>
    /// <param name="jobState"></param>
    /// <returns></returns>
    public static double GetProgressionPercentage(this IBackupJobState jobState) {
        if (jobState.TotalFilesToCopy == 0) return 0.0;

        double completedFiles = jobState.TotalFilesToCopy - jobState.FilesLeft;
        double progressPercentage = (completedFiles / jobState.TotalFilesToCopy) * 100.0;

        return Math.Max(0.0, Math.Min(100.0, progressPercentage));
    }
}