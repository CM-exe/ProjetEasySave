using ProjetEasySave.ViewModel;
using System.Windows;

namespace EasySaveWPF
{
    public partial class SaveProgressWindow : Window
    {
        private readonly ViewModel _viewModel;
        private readonly string _saveSpaceName;

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

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ResumeSave(_saveSpaceName);
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.PauseSave(_saveSpaceName);
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.StopSave(_saveSpaceName);
        }
    }
}