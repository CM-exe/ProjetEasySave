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

        public SaveProgressWindow(ViewModel viewModel, string saveSpaceName)
        {
            InitializeComponent();
            _viewModel = viewModel;
            _saveSpaceName = saveSpaceName;

            DataContext = _viewModel;

            Loaded += SaveProgressWindow_Loaded;
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
                if (_viewModel.CurrentFile.EndsWith(_viewModel.PausedSuffix))
                {
                    _viewModel.CurrentFile = _viewModel.CurrentFile.Replace(_viewModel.PausedSuffix, "");
                }
            }
            else
            {
                _viewModel.PauseSave(_saveSpaceName);
                PlayPauseButton.Content = "▶";
                ProgressBar.Foreground = Brushes.Gold;
                _isPaused = true;

                if (!_viewModel.CurrentFile.EndsWith(_viewModel.PausedSuffix))
                {
                    _viewModel.CurrentFile += _viewModel.PausedSuffix;
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPaused) { _viewModel.ResumeSave(_saveSpaceName); }
            _viewModel.StopSave(_saveSpaceName);
            ProgressBar.Foreground = Brushes.Red;

            if (_viewModel.CurrentFile.EndsWith(_viewModel.PausedSuffix))
            {
                _viewModel.CurrentFile = _viewModel.CurrentFile.Replace(_viewModel.PausedSuffix, "");
            }
            _viewModel.CurrentFile += _viewModel.StoppedSuffix;
        }
    }
}