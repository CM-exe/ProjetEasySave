using Microsoft.Win32;
using ProjetEasySave.Model;
using ProjetEasySave.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace EasySaveWPF
{
    public partial class MainWindow : Window
    {
        private readonly ViewModel _viewModel;
        private readonly ObservableCollection<SaveSpaceRow> _rows = new();

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new ViewModel();
            Loaded += OnLoaded;

            render();
        }

        private void render()
        {
            // Text of each elements
            btnAddSpace.Content = "➕ " + _viewModel.translate("AddSaveSpace");
            btnDeleteSpace.Content = "🗑 " + _viewModel.translate("RemoveSaveSpace");
            btnEditSpace.Content = "✏️ " + _viewModel.translate("EditSaveSpace");
            btnStartSave.Content = "▶ " + _viewModel.translate("StartSave");
            btnLanguage.Content = "🌐 " + _viewModel.translate("ChangeLanguage");
            btnLogsFormat.Content = "📝 " + _viewModel.translate("ChangeLogsFormat");
            textAppDescription.Text = _viewModel.translate("textAppDescription");
            textList.Text = _viewModel.translate("textList");
            textWorkspace.Text = _viewModel.translate("textWorkspace");
            headerName.Header = _viewModel.translate("Name");
            headerSource.Header = _viewModel.translate("Source");
            headerDestination.Header = _viewModel.translate("Destination");
            headerCompleteSavePath.Header = _viewModel.translate("CompleteSavePath");
            headerType.Header = _viewModel.translate("Type");
            headerState.Header = _viewModel.translate("State");
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {

            if (listSaveSpaces != null)
            {
                listSaveSpaces.ItemsSource = _rows;
                listSaveSpaces.SelectionChanged += (_, __) => UpdateButtonsState();
            }

            if (btnAddSpace != null) btnAddSpace.Click += OnAddClick;
            if (btnEditSpace != null) btnEditSpace.Click += OnEditClick;
            if (btnDeleteSpace != null) btnDeleteSpace.Click += OnDeleteClick;
            if (btnStartSave != null) btnStartSave.Click += OnStartClick;
            if (btnLanguage != null) btnLanguage.Click += OnLanguageClick;
            if (btnLogsFormat != null) btnLogsFormat.Click += OnLogsFormatClick;

            // Abonnement à l'événement updateTaskState pour chaque SaveSpace existant
            SubscribeToSaveSpaceEvents();

            RefreshList();
            UpdateButtonsState();
        }



        // Méthode pour s'abonner à l'événement updateTaskState de chaque SaveSpace
        private void SubscribeToSaveSpaceEvents()
        {
            var spaces = _viewModel.getSaveSpaces();
            if (spaces == null) return;

            foreach (var space in spaces)
            {
                // Désabonnement préalable pour éviter les doublons
                space.SaveTaskStateChanged -= SaveSpace_updateTaskState;
                space.SaveTaskStateChanged += SaveSpace_updateTaskState;
            }
        }

        // Gestionnaire d'événement appelé lors d'un changement d'état d'une tâche de sauvegarde
        private void SaveSpace_updateTaskState(object? sender, EventArgs e)
        {
            // Rafraîchir la liste sur le thread UI
            Dispatcher.Invoke(RefreshList);
        }

        private void RefreshList()
        {
            _rows.Clear();
            List<SaveSpace> spaces = _viewModel.getSaveSpaces();

            if (spaces == null)
            {
                return;
            }

            foreach (var space in spaces)
            {
                _rows.Add(new SaveSpaceRow
                {
                    Name = space.getName(),
                    SourcePath = space.getSourcePath(),
                    TargetPath = space.getDestinationPath(),
                    BackupType = space.getTypeSave(),
                    CompleteSavePath = space.getCompleteSavePath(),
                    State = space.getTaskStates().First().ToString() // <-- Récupération de l'état
                });
            }

            // Réabonnement aux événements pour les nouveaux SaveSpace
            SubscribeToSaveSpaceEvents();
        }

        private void UpdateButtonsState()
        {
            bool hasSelection = listSaveSpaces?.SelectedItem is SaveSpaceRow;
            if (btnEditSpace != null) btnEditSpace.IsEnabled = hasSelection;
            if (btnDeleteSpace != null) btnDeleteSpace.IsEnabled = hasSelection;
            if (btnStartSave != null) btnStartSave.IsEnabled = hasSelection;
        }

        private void OnAddClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveSpaceDialog(_viewModel.translate("AddSaveSpace"));
            if (dialog.ShowDialog() == true)
            {
                var input = dialog.Result;
                bool ok = _viewModel.addSaveSpace(input.Name, input.SourcePath, input.TargetPath, input.TypeSave, input.CompleteSavePath);
                ShowResult(ok, _viewModel.translate("SaveSpaceAdded"), _viewModel.translate("SaveSpaceAddFailed"));
                RefreshList();
            }
        }

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            if (listSaveSpaces?.SelectedItem is not SaveSpaceRow row)
            {
                return;
            }

            var space = _viewModel.getSaveSpaces().FirstOrDefault(s => s.getName() == row.Name);
            if (space == null)
            {
                MessageBox.Show(_viewModel.translate("InvalidSaveSpaceID"));
                return;
            }

            var dialog = new SaveSpaceDialog(_viewModel.translate("EditSaveSpace"))
            {
                Result = new SaveSpaceInput
                {
                    Name = space.getName(),
                    SourcePath = space.getSourcePath(),
                    TargetPath = space.getDestinationPath(),
                    TypeSave = space.getTypeSave(),
                    CompleteSavePath = space.getCompleteSavePath()
                }
            };

            if (dialog.ShowDialog() == true)
            {
                var input = dialog.Result;
                _viewModel.removeSaveSpace(space.getName());
                bool ok = _viewModel.addSaveSpace(input.Name, input.SourcePath, input.TargetPath, input.TypeSave, input.CompleteSavePath);
                ShowResult(ok, _viewModel.translate("SaveSpaceAdded"), _viewModel.translate("SaveSpaceAddFailed"));
                RefreshList();
            }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (listSaveSpaces?.SelectedItem is not SaveSpaceRow row)
            {
                return;
            }

            bool ok = _viewModel.removeSaveSpace(row.Name);
            ShowResult(ok, _viewModel.translate("SaveSpaceRemoved"), _viewModel.translate("SaveSpaceRemoveFailed"));
            RefreshList();
        }

        private async void OnStartClick(object sender, RoutedEventArgs e)
        {
            if (listSaveSpaces?.SelectedItem is not SaveSpaceRow row)
            {
                return;
            }

            if (false) // TODO: Check if the business software is running for this SaveSpace
            {
                // Display an error message and abort the flow
                MessageBox.Show(_viewModel.translate("ErrorBusinessSoftwareRunning"), "EasySave", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }


            //ShowResult(true, _viewModel.translate("SaveStarted"), _viewModel.translate("SaveStartFailed"));
            //bool ok = await _viewModel.startSave(row.Name);
            //ShowResult(ok, _viewModel.translate("SaveCompleted"), _viewModel.translate("SaveStartFailed"));

            var progressWindow = new SaveProgressWindow(_viewModel, row.Name)
            {
                Owner = this
            };
            progressWindow.ShowDialog();
        }

        private void OnLanguageClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SelectDialog(_viewModel.translate("LanguageCodePrompt"), _viewModel.translate("CurrentLanguage") + ": " + _viewModel.getLanguage(), _viewModel.translate("Language") + " :", ["en","fr"]);
            if (dialog.ShowDialog() == true)
            {
                // Change the language
                var code = dialog.Value.Trim();
                _viewModel.setLanguage(code);
                MessageBox.Show(
                    _viewModel.translate("Language") + ": " + _viewModel.getLanguage(),
                    "EasySave",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                RefreshList();
                render();
            }
        }

        private void OnLogsFormatClick(object sender, RoutedEventArgs e)
        {
            var dialog = new SelectDialog(_viewModel.translate("LogsFormatPrompt"), _viewModel.translate("CurrentLogsFormat") + ": " + _viewModel.getLogsFormat(), _viewModel.translate("LogsFormat") + " :", ["json", "xml"]);
            if (dialog.ShowDialog() == true)
            {
                // Change the logs format
                var format = dialog.Value.Trim();
                _viewModel.setLogsFormat(format);
                MessageBox.Show(
                    _viewModel.translate("CurrentLogsFormat") + ": " + _viewModel.getLogsFormat(),
                    "EasySave",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private void ShowResult(bool ok, string success, string fail)
        {
            MessageBox.Show(ok ? success : fail, "EasySave", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
        }

        private static T? FindFirstChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = FindFirstChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static Button? FindButtonByContent(DependencyObject parent, string content)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Button button && string.Equals(button.Content?.ToString(), content, StringComparison.OrdinalIgnoreCase))
                {
                    return button;
                }

                var result = FindButtonByContent(child, content);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private sealed class SaveSpaceRow
        {
            public string Name { get; init; } = string.Empty;
            public string SourcePath { get; init; } = string.Empty;
            public string TargetPath { get; init; } = string.Empty;

            public string BackupType { get; init; } = string.Empty;

            public string CompleteSavePath { get; init; } = string.Empty;

            public string State { get; init; } = string.Empty; // <-- Add State property
        }

        private sealed class SaveSpaceInput
        {
            public string Name { get; set; } = string.Empty;
            public string SourcePath { get; set; } = string.Empty;
            public string TargetPath { get; set; } = string.Empty;
            public string TypeSave { get; set; } = "complete";
            public string CompleteSavePath { get; set; } = string.Empty;
        }

        private sealed class SaveSpaceDialog : Window
        {
            private readonly TextBox _nameBox = new();
            private readonly TextBox _sourceBox = new();
            private readonly Button _sourceBrowserButton = new();
            private readonly TextBox _targetBox = new();
            private readonly Button _targetBrowserButton = new();
            private readonly ComboBox _typeBox = new();
            private readonly TextBox _completeBox = new();
            private readonly Button _completeBrowserButton = new();
            private readonly LanguageService _language = new LanguageService();

            public SaveSpaceInput Result { get; set; } = new();

            public SaveSpaceDialog(string title)
            {
                Title = title;
                Width = 420;
                Height = 320;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ResizeMode = ResizeMode.NoResize;

                var grid = new Grid { Margin = new Thickness(12) };
                for (int i = 0; i < 6; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                AddRow(grid, 0, _language.translate("Name"), _nameBox);

                ConfigureBrowseButton(_sourceBrowserButton, _sourceBox);
                AddRow(grid, 1, _language.translate("Source"), CreateBrowseRow(_sourceBox, _sourceBrowserButton));

                ConfigureBrowseButton(_targetBrowserButton, _targetBox);
                AddRow(grid, 2, _language.translate("Destination"), CreateBrowseRow(_targetBox, _targetBrowserButton));

                _typeBox.ItemsSource = new[] { "complete", "differential" };
                _typeBox.SelectedIndex = 0;
                _typeBox.SelectionChanged += (_, __) =>
                {
                    bool enabled = _typeBox.SelectedItem?.ToString() == "differential";
                    if (enabled)
                    { 
                        // Enable the row 4
                        _completeBox.IsEnabled = true;
                        _completeBrowserButton.IsEnabled = true;
                    }
                    else
                    {
                        // Disable the row 4 and clear its value
                        _completeBox.Text = string.Empty;
                        _completeBox.IsEnabled = false;
                        _completeBrowserButton.IsEnabled = false;
                    }
                };
                AddRow(grid, 3, "Type", _typeBox);

                // Default _completeBrowserButton is disabled because the default type is "complete"
                _completeBrowserButton.IsEnabled = false;
                ConfigureBrowseButton(_completeBrowserButton, _completeBox);
                AddRow(grid, 4, _language.translate("CompleteSavePath"), CreateBrowseRow(_completeBox, _completeBrowserButton));

                var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
                var ok = new Button { Content = _language.translate("Ok"), Width = 80, Margin = new Thickness(0, 0, 8, 0) };
                var cancel = new Button { Content = _language.translate("Cancel"), Width = 80 };
                ok.Click += (_, __) => { ApplyResult(); DialogResult = true; };
                cancel.Click += (_, __) => { DialogResult = false; };
                buttons.Children.Add(ok);
                buttons.Children.Add(cancel);

                Grid.SetRow(buttons, 5);
                Grid.SetColumnSpan(buttons, 2);
                grid.Children.Add(buttons);

                Content = grid;

                Loaded += (_, __) => LoadResult();
            }

            private static Grid CreateBrowseRow(TextBox textBox, Button button)
            {
                var innerGrid = new Grid();
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                textBox.Margin = new Thickness(0, 0, 8, 8);
                Grid.SetColumn(textBox, 0);
                innerGrid.Children.Add(textBox);

                button.Margin = new Thickness(0, 0, 0, 8);
                Grid.SetColumn(button, 1);
                innerGrid.Children.Add(button);

                return innerGrid;
            }

            private static void ConfigureBrowseButton(Button button, TextBox targetBox)
            {
                button.Content = "...";
                button.Width = 28;
                button.Click += (_, __) =>
                {
                    OpenFolderDialog dialog = new OpenFolderDialog();
                    bool? success = dialog.ShowDialog();
                    if (success == true)
                    {
                        targetBox.Text = dialog.FolderName;
                    }
                };
            }

            private void LoadResult()
            {
                _nameBox.Text = Result.Name;
                _sourceBox.Text = Result.SourcePath;
                _targetBox.Text = Result.TargetPath;
                _typeBox.SelectedItem = Result.TypeSave;
                _completeBox.Text = Result.CompleteSavePath;
                _completeBox.IsEnabled = Result.TypeSave == "differential";
            }

            private void ApplyResult()
            {
                Result.Name = _nameBox.Text.Trim();
                Result.SourcePath = _sourceBox.Text.Trim();
                Result.TargetPath = _targetBox.Text.Trim();
                Result.TypeSave = _typeBox.SelectedItem?.ToString() ?? "complete";
                Result.CompleteSavePath = _completeBox.Text.Trim();
            }

            private static void AddRow(Grid grid, int row, string label, FrameworkElement control)
            {
                var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 8) };
                control.Margin = new Thickness(0, 0, 0, 8);

                Grid.SetRow(text, row);
                Grid.SetColumn(text, 0);
                grid.Children.Add(text);

                Grid.SetRow(control, row);
                Grid.SetColumn(control, 1);
                grid.Children.Add(control);
            }
        }

        private sealed class InputDialog : Window
        {
            private readonly TextBox _inputBox = new();
            private readonly LanguageService _language = new LanguageService();

            public string Value { get; private set; } = string.Empty;

            public InputDialog(string title, string text, string label)
            {
                Title = title;
                Width = 360;
                Height = 200;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ResizeMode = ResizeMode.NoResize;

                var grid = new Grid { Margin = new Thickness(12) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var textBlock = new TextBlock
                {
                    Text = text,
                    Margin = new Thickness(0, 0, 0, 8),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(textBlock, 0);
                grid.Children.Add(textBlock);

                var labelBlock = new TextBlock
                {
                    Text = label,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                Grid.SetRow(labelBlock, 1);
                grid.Children.Add(labelBlock);

                _inputBox.Margin = new Thickness(0, 0, 0, 12);
                Grid.SetRow(_inputBox, 2);
                grid.Children.Add(_inputBox);

                var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                var ok = new Button { Content = _language.translate("Ok"), Width = 80, Margin = new Thickness(0, 0, 8, 0) };
                var cancel = new Button { Content = _language.translate("Cancel"), Width = 80 };
                ok.Click += (_, __) => { Value = _inputBox.Text; DialogResult = true; };
                cancel.Click += (_, __) => { DialogResult = false; };
                buttons.Children.Add(ok);
                buttons.Children.Add(cancel);

                Grid.SetRow(buttons, 3);
                grid.Children.Add(buttons);

                Content = grid;
            }
        }

        private sealed class SelectDialog : Window
        {
            private readonly ComboBox _comboBox = new();
            private readonly LanguageService _language = new LanguageService();

            public string Value { get; private set; } = string.Empty;

            public SelectDialog(string title, string text, string label, IEnumerable<string> options)
            {
                Title = title;
                Width = 360;
                Height = 200;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ResizeMode = ResizeMode.NoResize;

                var grid = new Grid { Margin = new Thickness(12) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var textBlock = new TextBlock
                {
                    Text = text,
                    Margin = new Thickness(0, 0, 0, 8),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(textBlock, 0);
                grid.Children.Add(textBlock);

                var labelBlock = new TextBlock
                {
                    Text = label,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                Grid.SetRow(labelBlock, 1);
                grid.Children.Add(labelBlock);

                _comboBox.ItemsSource = options;
                _comboBox.SelectedIndex = 0;
                _comboBox.Margin = new Thickness(0, 0, 0, 12);
                Grid.SetRow(_comboBox, 2);
                grid.Children.Add(_comboBox);

                var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                var ok = new Button { Content = _language.translate("Ok"), Width = 80, Margin = new Thickness(0, 0, 8, 0) };
                var cancel = new Button { Content = _language.translate("Cancel"), Width = 80 };
                ok.Click += (_, __) => { Value = _comboBox.SelectedItem?.ToString() ?? string.Empty; DialogResult = true; };
                cancel.Click += (_, __) => { DialogResult = false; };
                buttons.Children.Add(ok);
                buttons.Children.Add(cancel);

                Grid.SetRow(buttons, 3);
                grid.Children.Add(buttons);

                Content = grid;
            }
        }

    }
}