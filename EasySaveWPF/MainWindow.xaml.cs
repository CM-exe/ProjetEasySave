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
            btnConfig.Content = "⚙ " + _viewModel.translate("Config");
            btnAddSpace.Content = "➕ " + _viewModel.translate("AddSaveSpace");
            btnDeleteSpace.Content = "🗑 " + _viewModel.translate("RemoveSaveSpace");
            btnEditSpace.Content = "✏️ " + _viewModel.translate("EditSaveSpace");
            btnStartSave.Content = "▶ " + _viewModel.translate("StartSave");
            btnValidateEncryptKey.Content = "OK";
            textAppDescription.Text = _viewModel.translate("textAppDescription");
            textList.Text = _viewModel.translate("textList");
            textWorkspace.Text = _viewModel.translate("textWorkspace");
            headerName.Header = _viewModel.translate("Name");
            headerSource.Header = _viewModel.translate("Source");
            headerDestination.Header = _viewModel.translate("Destination");
            headerCompleteSavePath.Header = _viewModel.translate("CompleteSavePath");
            headerPriorityExt.Header = _viewModel.translate("PriorityExtensions");
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

            if (btnConfig != null) btnConfig.Click += OnConfigClick;
            if (btnAddSpace != null) btnAddSpace.Click += OnAddClick;
            if (btnEditSpace != null) btnEditSpace.Click += OnEditClick;
            if (btnDeleteSpace != null) btnDeleteSpace.Click += OnDeleteClick;
            if (btnStartSave != null) btnStartSave.Click += OnStartClick;

            // Abonnement à l'événement onSaveTaskStateChanged pour chaque SaveSpace existant
            SubscribeToSaveSpaceEvents();

            RefreshList();
            UpdateButtonsState();
        }



        // Méthode pour s'abonner à l'événement onSaveTaskStateChanged de chaque SaveSpace
        private void SubscribeToSaveSpaceEvents()
        {
            var spaces = _viewModel.getSaveSpaces();
            if (spaces == null) return;

            foreach (var space in spaces)
            {
                // Désabonnement préalable pour éviter les doublons
                space.SaveTaskStateChanged -= SaveSpace_onSaveTaskStateChanged;
                space.SaveTaskStateChanged += SaveSpace_onSaveTaskStateChanged;
            }
        }

        // Gestionnaire d'événement appelé lors d'un changement d'état d'une tâche de sauvegarde
        private void SaveSpace_onSaveTaskStateChanged(object? sender, EventArgs e)
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
                    PriorityExt = string.Join(", ", space.getPriorityExt()),
                    State = space.getTaskStates().First().ToString()
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
                bool ok = _viewModel.addSaveSpace(input.Name, input.SourcePath, input.TargetPath, input.TypeSave, input.PriorityExt.Select(p => p.StartsWith(".") ? p : "." + p).ToList(), input.CompleteSavePath);
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
                    CompleteSavePath = space.getCompleteSavePath(),
                    PriorityExt = space.getPriorityExt()
                }
            };

            if (dialog.ShowDialog() == true)
            {
                var input = dialog.Result;
                _viewModel.removeSaveSpace(space.getName());
                bool ok = _viewModel.addSaveSpace(input.Name, input.SourcePath, input.TargetPath, input.TypeSave, input.PriorityExt.Select(p => p.StartsWith(".") ? p : "." + p).ToList(), input.CompleteSavePath);
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
            // Collect selected rows (supports single or multiple selection now)
            var selectedRows = new List<SaveSpaceRow>();
            if (listSaveSpaces?.SelectedItems != null && listSaveSpaces.SelectedItems.Count > 0)
            {
                selectedRows.AddRange(listSaveSpaces.SelectedItems.OfType<SaveSpaceRow>());
            }
            else if (listSaveSpaces?.SelectedItem is SaveSpaceRow singleRow)
            {
                selectedRows.Add(singleRow);
            }

            if (selectedRows.Count == 0)
            {
                return;
            }

            // If multiple selected, ask confirmation
            if (selectedRows.Count > 1)
            {
                var confirm = MessageBox.Show(
                    _viewModel.translate("StartMultipleSavesConfirmation"),
                    "EasySave",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            // Global business-software check (keeps existing behavior)
            if (_viewModel.isBusinessSoftwareRunning()) // TODO: refine to check per SaveSpace if supported
            {
                MessageBox.Show(
                    _viewModel.translate("ErrorBusinessSoftwareRunning"),
                    "EasySave",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            // Start a progress window for each selected save space
            foreach (var saveRow in selectedRows)
            {
                var progressWindow = new SaveProgressWindow(_viewModel, saveRow.Name)
                {
                    Owner = this
                };
                progressWindow.Show();
            }
        }

        public void OnConfigClick(object sender, RoutedEventArgs e)
        {
            var dialog = new ConfigDialog(_viewModel, this);
            dialog.ShowDialog();
            RefreshList();
            render(); // Re-render to update texts if language was changed
        }

        private void SaveEncryptionKey_Click(object sender, RoutedEventArgs e)
        {
            // Retrieve the password directly from the UI element
            string newKey = EncryptionKeyPasswordBox.Password;

            if (!string.IsNullOrWhiteSpace(newKey))
            {
                // Send it to the ViewModel
                _viewModel.setEncryptionKey(newKey);

                // Optional: Clear the box or show a success message
                EncryptionKeyPasswordBox.Clear();
                MessageBox.Show("Encryption key successfully updated.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("The encryption key cannot be empty.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            public string PriorityExt { get; init; } = string.Empty;
        }

        private sealed class SaveSpaceInput
        {
            public string Name { get; set; } = string.Empty;
            public string SourcePath { get; set; } = string.Empty;
            public string TargetPath { get; set; } = string.Empty;
            public string TypeSave { get; set; } = "complete";
            public string CompleteSavePath { get; set; } = string.Empty;
            public List<string> PriorityExt { get; set; } = new List<string>();
        }

        private sealed class ConfigDialog : Window
        {
            private readonly LanguageService _language = LanguageService.getInstance();
            private readonly TextBlock _languageLabel = new();
            private readonly Button _languageButton = new();
            private readonly TextBlock _logsFormatLabel = new();
            private readonly Button _logsFormatButton = new();
            private readonly TextBlock _maxSizeLabel = new();
            private readonly TextBox _maxSizeTextBox = new();
            private readonly TextBlock _logsOnServerLabel = new();
            private readonly ToggleButton _logsOnServerToggle = new();
            private readonly TextBlock _logsOnLocalLabel = new();
            private readonly ToggleButton _logsOnLocalToggle = new();
            private readonly TextBlock _serverIpLabel = new();
            private readonly TextBox _serverIpTextBox = new();
            private readonly TextBlock _serverPortLabel = new();
            private readonly TextBox _serverPortTextBox = new();
            private readonly Button _reconnectToServerBtn = new();
            private readonly TextBlock _connectionToServerStateText = new();
            private readonly TextBlock _businessSoftareLabel = new();
            private readonly TextBox _businessSoftwareTextBox = new();
            private readonly Button ok = new();
            private readonly Button cancel = new();

            public ConfigDialog(ViewModel _viewModel, MainWindow _mainWindow)
            {
                Title = _language.translate("Config");
                Width = 420;
                Height = 350;
                MinHeight = Height;
                MinWidth = Width;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ResizeMode = ResizeMode.NoResize;

                var grid = new Grid { Margin = new Thickness(12) };

                // Row layout: 9 setting rows with small gaps, then spacer, then buttons row
                const int settingRows = 9;
                for (int i = 0; i < settingRows; i++)
                {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    if (i < settingRows-1)
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });
                }
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // spacer
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // buttons

                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                void RefreshTexts()
                {
                    Title = _language.translate("Config");
                    _languageLabel.Text = _language.translate("CurrentLanguage") + ": " + _language.getLanguage();
                    _languageButton.Content = "🌐 " + _language.translate("ChangeLanguage");
                    _logsFormatLabel.Text = _language.translate("CurrentLogsFormat") + ": " + _viewModel.getLogsFormat();
                    _logsFormatButton.Content = "📝 " + _language.translate("ChangeLogsFormat");
                    _maxSizeLabel.Text = _language.translate("MaxSize") + " (Ko):";
                    _logsOnServerLabel.Text = _language.translate("LogsOnServer");
                    _logsOnServerToggle.Content = _logsOnServerToggle.IsChecked == true ? _language.translate("Enabled") : _language.translate("Disabled");
                    _logsOnLocalLabel.Text = _language.translate("LogsOnLocal");
                    _logsOnLocalToggle.Content = _logsOnLocalToggle.IsChecked == true ? _language.translate("Enabled") : _language.translate("Disabled");
                    _serverIpLabel.Text = _language.translate("ServerIp");
                    _serverPortLabel.Text = _language.translate("ServerPort");
                    _reconnectToServerBtn.Content = "🔌 " + _language.translate("ReconnectToServer");
                    _connectionToServerStateText.Text = _viewModel.isConnectedToServer() ? _language.translate("Connected") : _language.translate("Disconnected");
                    _businessSoftareLabel.Text = _language.translate("BusinessSoftware");
                    ok.Content = _language.translate("Ok");
                    cancel.Content = _language.translate("Close");
                }

                // Initial toggle states
                _logsOnServerToggle.IsChecked = _viewModel.getBoolLogsOnServer();
                _logsOnLocalToggle.IsChecked = _viewModel.getBoolLogsOnLocal();

                RefreshTexts();

                // Language row (row 0)
                Grid.SetRow(_languageLabel, 0);
                Grid.SetColumn(_languageLabel, 0);
                _languageLabel.VerticalAlignment = VerticalAlignment.Center;
                grid.Children.Add(_languageLabel);

                Grid.SetRow(_languageButton, 0);
                Grid.SetColumn(_languageButton, 1);
                _languageButton.Margin = new Thickness(0, 0, 0, 0);
                grid.Children.Add(_languageButton);

                // Logs format row (row 2)
                Grid.SetRow(_logsFormatLabel, 2);
                Grid.SetColumn(_logsFormatLabel, 0);
                _logsFormatLabel.VerticalAlignment = VerticalAlignment.Center;
                grid.Children.Add(_logsFormatLabel);

                Grid.SetRow(_logsFormatButton, 2);
                Grid.SetColumn(_logsFormatButton, 1);
                grid.Children.Add(_logsFormatButton);

                // Max size row (row 4)
                Grid.SetRow(_maxSizeLabel, 4);
                Grid.SetColumn(_maxSizeLabel, 0);
                _maxSizeLabel.VerticalAlignment = VerticalAlignment.Center;
                grid.Children.Add(_maxSizeLabel);

                Grid.SetRow(_maxSizeTextBox, 4);
                Grid.SetColumn(_maxSizeTextBox, 1);
                _maxSizeTextBox.Width = 100;
                grid.Children.Add(_maxSizeTextBox);

                // Logs on server row (row 6)
                Grid.SetRow(_logsOnServerLabel, 6);
                Grid.SetColumn(_logsOnServerLabel, 0);
                _logsOnServerLabel.VerticalAlignment = VerticalAlignment.Center;
                grid.Children.Add(_logsOnServerLabel);

                Grid.SetRow(_logsOnServerToggle, 6);
                Grid.SetColumn(_logsOnServerToggle, 1);
                _logsOnServerToggle.HorizontalAlignment = HorizontalAlignment.Center;
                _logsOnServerToggle.Margin = new Thickness(0);
                grid.Children.Add(_logsOnServerToggle);

                // Logs on local row (row 8)
                Grid.SetRow(_logsOnLocalLabel, 8);
                Grid.SetColumn(_logsOnLocalLabel, 0);
                _logsOnLocalLabel.VerticalAlignment = VerticalAlignment.Center;
                grid.Children.Add(_logsOnLocalLabel);

                Grid.SetRow(_logsOnLocalToggle, 8);
                Grid.SetColumn(_logsOnLocalToggle, 1);
                _logsOnLocalToggle.HorizontalAlignment = HorizontalAlignment.Center;
                _logsOnLocalToggle.Margin = new Thickness(0);
                grid.Children.Add(_logsOnLocalToggle);

                // Server IP row (row 10)
                Grid.SetRow(_serverIpLabel, 10);
                Grid.SetColumn(_serverIpLabel, 0);
                _serverIpLabel.VerticalAlignment = VerticalAlignment.Center;
                grid.Children.Add(_serverIpLabel);
                
                Grid.SetRow(_serverIpTextBox, 10);
                Grid.SetColumn(_serverIpTextBox, 1);
                _serverIpTextBox.Width = 150;
                grid.Children.Add(_serverIpTextBox);

                // Server port row (row 12)
                Grid.SetRow(_serverPortLabel, 12);
                Grid.SetColumn(_serverPortLabel, 0);
                _serverPortLabel.VerticalAlignment = VerticalAlignment.Center;
                grid.Children.Add(_serverPortLabel);

                Grid.SetRow(_serverPortTextBox, 12);
                Grid.SetColumn(_serverPortTextBox, 1);
                _serverPortTextBox.Width = 100;
                grid.Children.Add(_serverPortTextBox);

                // Reconnect to server button row (row 14)
                Grid.SetRow(_reconnectToServerBtn, 14);
                Grid.SetColumn(_reconnectToServerBtn, 0);
                _reconnectToServerBtn.HorizontalAlignment = HorizontalAlignment.Center;
                grid.Children.Add(_reconnectToServerBtn);

                Grid.SetRow(_connectionToServerStateText, 14);
                Grid.SetColumn(_connectionToServerStateText, 1);
                _connectionToServerStateText.HorizontalAlignment = HorizontalAlignment.Center;
                grid.Children.Add(_connectionToServerStateText);

                // Business software row (row 16)
                Grid.SetRow(_businessSoftareLabel, 16);
                Grid.SetColumn(_businessSoftareLabel, 0);
                _businessSoftareLabel.VerticalAlignment = VerticalAlignment.Center;
                grid.Children.Add(_businessSoftareLabel);

                Grid.SetRow(_businessSoftwareTextBox, 16);
                Grid.SetColumn(_businessSoftwareTextBox, 1);
                _businessSoftwareTextBox.HorizontalAlignment = HorizontalAlignment.Center;
                grid.Children.Add(_businessSoftwareTextBox);

                // Language button click
                _languageButton.Click += (_, __) =>
                {
                    var dialog = new SelectDialog(
                    _language.translate("LanguageCodePrompt"),
                    _language.translate("CurrentLanguage") + ": " + _language.getLanguage(),
                    _language.translate("Language") + " :",
                    new[] { "en", "fr" }
                    );
                    if (dialog.ShowDialog() == true)
                    {
                    var code = dialog.Value.Trim();
                    _language.setLanguage(code);
                    RefreshTexts();
                    _mainWindow.render(); // Re-render main window to update all texts
                    MessageBox.Show(
                        _language.translate("Language") + ": " + _language.getLanguage(),
                        "EasySave",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    }
                };

                // Logs format button click
                _logsFormatButton.Click += (_, __) =>
                {
                    var dialog = new SelectDialog(
                    _language.translate("LogsFormatPrompt"),
                    _language.translate("CurrentLogsFormat") + ": " + _viewModel.getLogsFormat(),
                    _language.translate("LogsFormat") + " :",
                    new[] { "json", "xml" }
                    );
                    if (dialog.ShowDialog() == true)
                    {
                    var format = dialog.Value.Trim();
                    _viewModel.setLogsFormat(format);
                    RefreshTexts();
                    MessageBox.Show(
                        _language.translate("CurrentLogsFormat") + ": " + _viewModel.getLogsFormat(),
                        "EasySave",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    }
                };

                // Set initial value for max size
                _maxSizeTextBox.Text = _viewModel.getMaxSize().ToString();

                // Only allow numbers in _maxSizeTextBox
                _maxSizeTextBox.PreviewTextInput += (s, e) =>
                {
                    e.Handled = !e.Text.All(char.IsDigit);
                };
                DataObject.AddPastingHandler(_maxSizeTextBox, (s, e) =>
                {
                    if (e is DataObjectPastingEventArgs args && args.DataObject.GetDataPresent(DataFormats.Text))
                    {
                    string text = (string)args.DataObject.GetData(DataFormats.Text);
                    if (!text.All(char.IsDigit))
                    {
                        args.CancelCommand();
                    }
                    }
                    else
                    {
                    // If not text, cancel
                    if (e is DataObjectPastingEventArgs args2) args2.CancelCommand();
                    }
                });
                // Validate and update config on lost focus
                _maxSizeTextBox.LostFocus += (s, e) =>
                {
                    if (int.TryParse(_maxSizeTextBox.Text.Trim(), out int maxSize))
                    {
                    _viewModel.setMaxSize(maxSize);
                    RefreshTexts();
                    }
                    else
                    {
                    _maxSizeTextBox.Text = _viewModel.getMaxSize().ToString();
                    MessageBox.Show(
                        _language.translate("InvalidMaxSize"),
                        "EasySave",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    }
                };

                // Logs on server toggle
                _logsOnServerToggle.Checked += (s, e) => { _viewModel.setBoolLogsOnServer(true); RefreshTexts(); };
                _logsOnServerToggle.Unchecked += (s, e) => { _viewModel.setBoolLogsOnServer(false); RefreshTexts(); };

                // Logs on local toggle
                _logsOnLocalToggle.Checked += (s, e) => { _viewModel.setBoolLogsOnLocal(true); RefreshTexts(); };
                _logsOnLocalToggle.Unchecked += (s, e) => { _viewModel.setBoolLogsOnLocal(false); RefreshTexts(); };

                // Set initial value for server IP
                _serverIpTextBox.Text = _viewModel.getServerIp();

                // Only allow ip format
                _serverIpTextBox.PreviewTextInput += (s, e) =>
                {
                    e.Handled = !char.IsDigit(e.Text, 0) && e.Text != ".";
                };
                DataObject.AddPastingHandler(_serverIpTextBox, (s, e) =>
                {
                    if (e is DataObjectPastingEventArgs args && args.DataObject.GetDataPresent(DataFormats.Text))
                    {
                    string text = (string)args.DataObject.GetData(DataFormats.Text);
                    if (!text.All(c => char.IsDigit(c) || c == '.'))
                    {
                        args.CancelCommand();
                    }
                    }
                    else
                    {
                    // If not text, cancel
                    if (e is DataObjectPastingEventArgs args2) args2.CancelCommand();
                    }
                });
                // Validate and update config on lost focus
                _serverIpTextBox.LostFocus += (s, e) =>
                {
                    string ip = _serverIpTextBox.Text.Trim();
                    if (System.Net.IPAddress.TryParse(ip, out _))
                    {
                        _viewModel.setServerIp(ip);
                    }
                    else
                    {
                        _serverIpTextBox.Text = _viewModel.getServerIp();
                        MessageBox.Show(
                            _language.translate("InvalidServerIp"),
                            "EasySave",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                    }
                };

                // Set initial value for server port
                _serverPortTextBox.Text = _viewModel.getServerPort().ToString();

                // Only allow numbers in _serverPortTextBox
                _serverPortTextBox.PreviewTextInput += (s, e) =>
                {
                    e.Handled = !e.Text.All(char.IsDigit);
                };
                DataObject.AddPastingHandler(_serverPortTextBox, (s, e) =>
                {
                    if (e is DataObjectPastingEventArgs args && args.DataObject.GetDataPresent(DataFormats.Text))
                    {
                    string text = (string)args.DataObject.GetData(DataFormats.Text);
                    if (!text.All(char.IsDigit))
                    {
                        args.CancelCommand();
                    }
                    }
                    else
                    {
                    // If not text, cancel
                    if (e is DataObjectPastingEventArgs args2) args2.CancelCommand();
                    }
                });
                // Validate and update config on lost focus
                _serverPortTextBox.LostFocus += (s, e) =>
                {
                    if (int.TryParse(_serverPortTextBox.Text.Trim(), out int port) && port >= 0 && port <= 65535)
                    {
                        _viewModel.setServerPort(port);
                    }
                    else
                    {
                        _serverPortTextBox.Text = _viewModel.getServerPort().ToString();
                        MessageBox.Show(
                            _language.translate("InvalidServerPort"),
                            "EasySave",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                    }
                };

                // Reconnect to server button click
                _reconnectToServerBtn.Click += (_, __) =>
                {
                    _connectionToServerStateText.Text = _language.translate("Connecting");
                    _viewModel.reconnectToServer();
                    RefreshTexts();
                };

                // Business software text box
                _businessSoftwareTextBox.Text = _viewModel.getBusinessSoftwareName();
                _businessSoftwareTextBox.LostFocus += (s, e) =>
                {
                    string name = _businessSoftwareTextBox.Text.Trim();
                    _viewModel.setBusinessSoftwareName(name);
                };


                // Buttons (row after spacer)
                var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
                ok.Width = 80;
                ok.Margin = new Thickness(0, 0, 8, 0);
                cancel.Width = 80;
                ok.Click += (_, __) => { DialogResult = true; Close(); };
                cancel.Click += (_, __) => { DialogResult = false; Close(); };
                buttonsPanel.Children.Add(ok);
                buttonsPanel.Children.Add(cancel);

                Grid.SetRow(buttonsPanel, grid.RowDefinitions.Count - 1);
                Grid.SetColumnSpan(buttonsPanel, 2);
                grid.Children.Add(buttonsPanel);

                Content = grid;
            }
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
            private readonly TextBox _priorityBox = new();
            private readonly Button _completeBrowserButton = new();
            private readonly LanguageService _language = LanguageService.getInstance();

            public SaveSpaceInput Result { get; set; } = new();

            public SaveSpaceDialog(string title)
            {
                Title = title;
                Width = 420;
                Height = 320;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ResizeMode = ResizeMode.NoResize;

                var grid = new Grid { Margin = new Thickness(12) };
                for (int i = 0; i < 7; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
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

                AddRow(grid, 5, _language.translate("PriorityExtensions"), _priorityBox);

                var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
                var ok = new Button { Content = _language.translate("Ok"), Width = 80, Margin = new Thickness(0, 0, 8, 0) };
                var cancel = new Button { Content = _language.translate("Cancel"), Width = 80 };
                ok.Click += (_, __) => { ApplyResult(); DialogResult = true; };
                cancel.Click += (_, __) => { DialogResult = false; };
                buttons.Children.Add(ok);
                buttons.Children.Add(cancel);

                Grid.SetRow(buttons, 6);
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
                _priorityBox.Text = string.Join(", ", Result.PriorityExt);
            }

            private void ApplyResult()
            {
                Result.Name = _nameBox.Text.Trim();
                Result.SourcePath = _sourceBox.Text.Trim();
                Result.TargetPath = _targetBox.Text.Trim();
                Result.TypeSave = _typeBox.SelectedItem?.ToString() ?? "complete";
                Result.CompleteSavePath = _completeBox.Text.Trim();
                Result.PriorityExt = _priorityBox.Text.Split(",").Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
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
            private readonly LanguageService _language = LanguageService.getInstance();

            public string Value { get; private set; } = string.Empty;

            public InputDialog(string title, string text, string label)
            {
                Title = title;
                Width = 380;
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
            private readonly LanguageService _language = LanguageService.getInstance();

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