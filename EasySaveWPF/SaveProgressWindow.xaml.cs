using ProjetEasySave.ViewModel;
using System;
using System.Windows;
using System.Windows.Media;

namespace EasySaveWPF
{
    /// <summary>
    /// Interaction logic for SaveProgressWindow.xaml.
    /// Handles the UI representation of a backup job, including progress tracking, pausing, and stopping functionalities.
    /// </summary>
    public partial class SaveProgressWindow : Window
    {
        private readonly ViewModel _viewModel;
        private readonly string _saveSpaceName;
        private bool _isPaused = false;

        /// <summary>
        /// Gets the main view model. Exposed publicly to allow XAML data binding.
        /// </summary>
        /// <value>The <see cref="ViewModel"/> instance managing the application state.</value>
        public ViewModel MainViewModel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveProgressWindow"/> class.
        /// </summary>
        /// <param name="viewModel">The main view model managing the application logic.</param>
        /// <param name="saveSpaceName">The name of the save space associated with this progress window.</param>
        public SaveProgressWindow(ViewModel viewModel, string saveSpaceName)
        {
            MainViewModel = viewModel;

            InitializeComponent();

            _viewModel = viewModel;
            _saveSpaceName = saveSpaceName;

            // Set the save space name in the view
            SaveNameTextBlock.Text = _saveSpaceName;

            DataContext = _viewModel.GetJobState(_saveSpaceName);

            _viewModel.BusinessSoftwareStateChanged += OnBusinessSoftwareStateChanged;

            Loaded += SaveProgressWindow_Loaded;
            Closed += SaveProgressWindow_Closed;
        }

        /// <summary>
        /// Handles the event triggered when a business software state changes (e.g., software is launched or closed).
        /// Automatically pauses or unlocks the backup process.
        /// </summary>
        /// <param name="isPausedBySoftware">Indicates whether the backup should be paused due to running business software.</param>
        private void OnBusinessSoftwareStateChanged(bool isPausedBySoftware)
        {
            // Must use Dispatcher to update UI from a background thread
            Dispatcher.Invoke(() =>
            {
                if (isPausedBySoftware)
                {
                    PlayPauseButton.Content = "▶";
                    PlayPauseButton.IsEnabled = false; // Block the user from manually resuming
                    ProgressBar.Foreground = Brushes.Gold;
                    _isPaused = true;
                }
                else
                {
                    PlayPauseButton.Content = "⏸";
                    PlayPauseButton.IsEnabled = true; // Unlock the button
                    ProgressBar.Foreground = Brushes.Green;
                    _isPaused = false;
                }
            });
        }

        /// <summary>
        /// Handles the Window Closed event.
        /// Unsubscribes from ViewModel events to prevent memory leaks.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void SaveProgressWindow_Closed(object? sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.BusinessSoftwareStateChanged -= OnBusinessSoftwareStateChanged;
            }
        }

        /// <summary>
        /// Handles the Window Loaded event.
        /// Starts the asynchronous backup process and displays the result message upon completion or failure.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private async void SaveProgressWindow_Loaded(object sender, RoutedEventArgs e)
        {
            bool success = await _viewModel.StartSaveAsync(_saveSpaceName);

            if (success)
            {
                MessageBox.Show(
                    _viewModel.SaveCompletedMessage,
                    "EasySave",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            else
            {
                MessageBox.Show(
                    _viewModel.SaveStoppedMessage,
                    "EasySave",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            Close();
        }

        /// <summary>
        /// Handles the click event of the Play/Pause button.
        /// Toggles the backup state between paused and running.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
            {
                _viewModel.ResumeSave(_saveSpaceName);
                PlayPauseButton.Content = "⏸";
                ProgressBar.Foreground = Brushes.Green;
                _isPaused = false;
            }
            else
            {
                _viewModel.PauseSave(_saveSpaceName);
                PlayPauseButton.Content = "▶";
                ProgressBar.Foreground = Brushes.Gold;
                _isPaused = true;
            }
        }

        /// <summary>
        /// Handles the click event of the Stop button.
        /// Cancels the current backup job, updates the progress UI, and appends the stopped suffix to the current file.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve the specific state for this job
            var state = _viewModel.GetJobState(_saveSpaceName);

            if (_isPaused) { _viewModel.ResumeSave(_saveSpaceName); }
            _viewModel.StopSave(_saveSpaceName);
            ProgressBar.Foreground = Brushes.Red;

            // Apply suffixes to the specific state, using the ViewModel's translation strings
            if (state.CurrentFile != null)
            {
                if (state.CurrentFile.EndsWith(_viewModel.PausedSuffix))
                {
                    state.CurrentFile = state.CurrentFile.Replace(_viewModel.PausedSuffix, "");
                }
                else if (state.CurrentFile.EndsWith(_viewModel.PausePendingSuffix))
                {
                    state.CurrentFile = state.CurrentFile.Replace(_viewModel.PausePendingSuffix, "");
                }

                state.CurrentFile += _viewModel.StoppedSuffix;
            }
        }
    }
}