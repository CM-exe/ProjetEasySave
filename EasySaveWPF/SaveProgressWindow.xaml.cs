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
            try
            {
                await _viewModel.StartSaveAsync(_saveSpaceName);

                MessageBox.Show(
                    "Sauvegarde terminée avec succès",
                    "EasySave",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show(
                    "Sauvegarde interrompue",
                    "EasySave",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
            finally
            {
                Close();
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if(_isPaused)
            {
                // Reprendre
                _viewModel.ResumeSave(_saveSpaceName);
                PlayPauseButton.Content = "⏸";
                ProgressBar.Foreground = Brushes.Green;
                _isPaused = false;
            }
            else
            {
                // Mettre en pause
                _viewModel.PauseSave(_saveSpaceName);
                PlayPauseButton.Content = "▶";
                ProgressBar.Foreground = Brushes.Gold;
                _isPaused = true;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.StopSave(_saveSpaceName);
        }
    }
}