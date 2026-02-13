using EasySave.Logger;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EasySave.Views;
public partial class Logs : INotifyPropertyChanged {
    public IViewModel ViewModel { get; private set; }
    public ObservableCollection<Log> LogCollection { get; private set; } = [];
    public ObservableCollection<Log> PagedLogCollection { get; private set; } = [];
    public ObservableCollection<string> LogLevels { get; private set; } = [];
    public int PageSize { get; set; } = 25;
    private int _currentPage = 1;
    public int CurrentPage {
        get => _currentPage;
        set {
            if (_currentPage != value) {
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
                UpdatePagedLogCollection();
            }
        }
    }
    public int TotalPages => (LogCollection.Count + PageSize - 1) / PageSize;
    public bool IsPreviousEnabled => CurrentPage > 1;
    public bool IsNextEnabled => CurrentPage < TotalPages;
    public bool IsFirstEnabled => CurrentPage > 1;
    public bool IsLastEnabled => CurrentPage < TotalPages;
    public string _search = "";
    public string Search {
        get => _search;
        set {
            if (_search != value) {
                _search = value;
                OnPropertyChanged(nameof(Search));
            }
        }
    }
    public string? _FilteredLogLevel = null;
    public string? FilteredLogLevel {
        get => _FilteredLogLevel;
        set {
            if (_FilteredLogLevel != value && value is not null) {
                _FilteredLogLevel = value;
                OnPropertyChanged(nameof(FilteredLogLevel));
                UpdateLogCollection();
            }
        }
    }

    // https://stackoverflow.com/a/1268648
    private static readonly Regex _NumberRegex = new("[^0-9.-]+");
    private static bool _IsNumericInput(string text) {
        return !_NumberRegex.IsMatch(text);
    }
    private void NumberBoxPasting(object sender, DataObjectPastingEventArgs e) {
        if (e.DataObject.GetDataPresent(typeof(String))) {
            String text = (String)e.DataObject.GetData(typeof(String));
            if (!_IsNumericInput(text)) {
                e.CancelCommand();
            }
        } else {
            e.CancelCommand();
        }
    }
    private void PreviewNumberInput(object sender, TextCompositionEventArgs e) {
        if (sender is System.Windows.Controls.TextBox textBox) {
            if (!_IsNumericInput(e.Text) || string.IsNullOrEmpty(e.Text)) {
                e.Handled = true; // Prevent non-numeric input
                return;
            }
            // Optionally, you can limit the length of the input
            if (textBox.Text.Length >= 5) { // Example: limit to 5 characters
                e.Handled = true;
                return;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Logs(IViewModel viewModel) {
        this.ViewModel = viewModel;

        InitializeComponent();

        UpdateLogCollection();
        MainGrid.DataContext = this;

        //Task.Run(() => {
        //    while (true) {
        //        List<Log> logs = ReadLogs();

        //        if (logs.Count > LogCollection.Count) {
        //            this.Dispatcher.Invoke(() => {
        //                UpdateLogCollection(logs);
        //            });
        //        }

        //        Thread.Sleep(1000);
        //    }
        //});
    }

    private List<Log> ReadLogs() {
        var logReader = new LogFileJSON();
        return logReader.Read(this.ViewModel.Configuration.LogFile);
    }

    private void UpdatePagedLogCollection() {
        PagedLogCollection.Clear();
        IEnumerable<Log> logs = LogCollection.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
        foreach (Log log in logs) {
            PagedLogCollection.Add(log);
        }

        if (CurrentPage > TotalPages) {
            CurrentPage = 1;
        }

        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(IsPreviousEnabled));
        OnPropertyChanged(nameof(IsNextEnabled));
        OnPropertyChanged(nameof(IsFirstEnabled));
        OnPropertyChanged(nameof(IsLastEnabled));
    }

    private void UpdateLogCollection(IEnumerable<Log>? logs = null) {
        LogCollection.Clear();
        logs ??= ReadLogs();

        if (!string.IsNullOrEmpty(Search)) {
            var searchPattern = Search.ToLowerInvariant();
            logs = logs.Where(log => log.Message.Contains(searchPattern, StringComparison.InvariantCultureIgnoreCase) ||
                                                  log.Level.ToString().Contains(searchPattern, StringComparison.InvariantCultureIgnoreCase) ||
                                                  log.Destination.Contains(searchPattern, StringComparison.CurrentCultureIgnoreCase) ||
                                                  log.Source.Contains(searchPattern, StringComparison.CurrentCultureIgnoreCase) ||
                                                  log.JobName.Contains(searchPattern, StringComparison.CurrentCultureIgnoreCase));
        }

        LogLevels.Clear();
        foreach (var level in logs.Select(l => l.Level.ToString()).Distinct()) {
            LogLevels.Add(level);
        }

        if (
            !string.IsNullOrEmpty(FilteredLogLevel) &&
            !string.IsNullOrWhiteSpace(FilteredLogLevel) &&
            LogLevels.Contains(FilteredLogLevel, StringComparer.CurrentCultureIgnoreCase)
        ) {
            logs = LogCollection.Where(log => log.Level.ToString().Equals(FilteredLogLevel, StringComparison.CurrentCultureIgnoreCase));
        }

        foreach (Log log in logs) {
            LogCollection.Add(log);
        }

        UpdatePagedLogCollection();
    }

    private void PreviousPage_Click(object sender, RoutedEventArgs e) {
        if (CurrentPage > 1)
            CurrentPage--;
    }

    private void NextPage_Click(object sender, RoutedEventArgs e) {
        if (CurrentPage < TotalPages)
            CurrentPage++;
    }

    private void FirstPage_Click(object sender, RoutedEventArgs e) {
        if (CurrentPage > 1)
            CurrentPage = 1;
    }

    private void LastPage_Click(object sender, RoutedEventArgs e) {
        if (CurrentPage < TotalPages)
            CurrentPage = TotalPages;
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
        var grid = sender as DataGrid;
        if (grid?.SelectedItem is Log selectedLog) {
            var detailWindow = new LogDetailsWindow(selectedLog, this.ViewModel);
            detailWindow.ShowDialog(); // modal
        }
    }

    protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void SearchButton_Click(object sender, RoutedEventArgs e) {
        UpdateLogCollection();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) {
        UpdateLogCollection();
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e) {
        var logReader = new LogFileJSON();
        logReader.Clear(this.ViewModel.Configuration.LogFile);
        UpdateLogCollection();
    }
}
