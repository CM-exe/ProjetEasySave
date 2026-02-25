using ProjetEasySave.ViewModel;
using System.Windows;
using System.Windows.Media;

namespace EasySaveWPF
{
    public partial class SaveProgressWindow : Window
    {
        private readonly ViewModel _viewModel;
        private readonly string _saveSpaceName;
        private bool _isPaused = false;

        // Expose the ViewModel publicly so XAML can bind to it
        public ViewModel MainViewModel { get; private set; }

        public SaveProgressWindow(ViewModel viewModel, string saveSpaceName)
        {
            MainViewModel = viewModel;

            InitializeComponent();

            _viewModel = viewModel;
            _saveSpaceName = saveSpaceName;


            DataContext = _viewModel.GetJobState(_saveSpaceName);

            _viewModel.BusinessSoftwareStateChanged += OnBusinessSoftwareStateChanged;

            Loaded += SaveProgressWindow_Loaded;
            Closed += SaveProgressWindow_Closed;
        }

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

        private void SaveProgressWindow_Closed(object? sender, EventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.BusinessSoftwareStateChanged -= OnBusinessSoftwareStateChanged;  
            }
        }

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

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            //if (_isPaused) { _viewModel.ResumeSave(_saveSpaceName); }
            //_viewModel.StopSave(_saveSpaceName);
            //ProgressBar.Foreground = Brushes.Red;

            //if (_viewModel.CurrentFile.EndsWith(_viewModel.PausedSuffix))
            //{
            //    _viewModel.CurrentFile = _viewModel.CurrentFile.Replace(_viewModel.PausedSuffix, "");
            //}
            //_viewModel.CurrentFile += _viewModel.StoppedSuffix;

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