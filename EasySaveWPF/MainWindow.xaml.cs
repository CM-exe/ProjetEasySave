using ProjetEasySave.Model;
using ProjetEasySave.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EasySaveWPF
{
    public partial class MainWindow : Window
    {
        private readonly ViewModel _viewModel;
        private ListView? _listView;
        private Button? _addButton;
        private Button? _editButton;
        private Button? _deleteButton;
        private readonly ObservableCollection<SaveSpaceRow> _rows = new();

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new ViewModel();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _listView = FindFirstChild<ListView>(this);
            _addButton = FindButtonByContent(this, "Ajouter");
            _editButton = FindButtonByContent(this, "Éditer");
            _deleteButton = FindButtonByContent(this, "Supprimer");

            if (_listView != null)
            {
                _listView.ItemsSource = _rows;
                _listView.SelectionChanged += (_, __) => UpdateButtonsState();
            }

            if (_addButton != null) _addButton.Click += OnAddClick;
            if (_editButton != null) _editButton.Click += OnEditClick;
            if (_deleteButton != null) _deleteButton.Click += OnDeleteClick;

            RefreshList();
            UpdateButtonsState();
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
                    TargetPath = space.getDestinationPath()
                });
            }
        }

        private void UpdateButtonsState()
        {
            bool hasSelection = _listView?.SelectedItem is SaveSpaceRow;
            if (_editButton != null) _editButton.IsEnabled = hasSelection;
            if (_deleteButton != null) _deleteButton.IsEnabled = hasSelection;
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
            if (_listView?.SelectedItem is not SaveSpaceRow row)
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
            if (_listView?.SelectedItem is not SaveSpaceRow row)
            {
                return;
            }

            bool ok = _viewModel.removeSaveSpace(row.Name);
            ShowResult(ok, _viewModel.translate("SaveSpaceRemoved"), _viewModel.translate("SaveSpaceRemoveFailed"));
            RefreshList();
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
            private readonly TextBox _targetBox = new();
            private readonly ComboBox _typeBox = new();
            private readonly TextBox _completeBox = new();

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

                AddRow(grid, 0, "Nom", _nameBox);
                AddRow(grid, 1, "Source", _sourceBox);
                AddRow(grid, 2, "Destination", _targetBox);

                _typeBox.ItemsSource = new[] { "complete", "differential" };
                _typeBox.SelectedIndex = 0;
                _typeBox.SelectionChanged += (_, __) => _completeBox.IsEnabled = (_typeBox.SelectedItem?.ToString() == "differential");
                AddRow(grid, 3, "Type", _typeBox);

                AddRow(grid, 4, "Chemin complet", _completeBox);

                var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
                var ok = new Button { Content = "OK", Width = 80, Margin = new Thickness(0, 0, 8, 0) };
                var cancel = new Button { Content = "Annuler", Width = 80 };
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

            private static void AddRow(Grid grid, int row, string label, Control control)
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
    }
}