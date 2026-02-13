using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace EasySave.Views {
    /// <summary>
    /// Interaction logic for SelectProcess.xaml
    /// </summary>
    public partial class SelectProcess : Window {
        public class ProcessInfo {
            public string Name { get; set; }
            public int Id { get; set; }
            public string Path { get; set; }
        }

        public IViewModel ViewModel { get; private set; }
        public List<string> SelectedProcesses { get; private set; } = [];

        private ObservableCollection<ProcessInfo> _allProcesses = new ObservableCollection<ProcessInfo>();

        public SelectProcess(IViewModel viewModel) {
            this.ViewModel = viewModel;
            InitializeComponent();
            LoadProcesses();
            ProcessListView.ItemsSource = _allProcesses;
            ProcessListView.SelectionChanged += ProcessListView_SelectionChanged;
            OkButton.IsEnabled = false;
            this.MainStackPanel.DataContext = this;
        }

        private void LoadProcesses() {
            _allProcesses.Clear();
            foreach (var proc in Process.GetProcesses().OrderBy(p => p.ProcessName)) {
                try {
                    _allProcesses.Add(new ProcessInfo {
                        Name = proc.ProcessName,
                        Id = proc.Id,
                        Path = proc.MainModule?.FileName ?? ""
                    });
                } catch {
                    // Some processes may not allow access to MainModule
                }
            }
        }

        private void ApplySearchFilter() {
            SearchBox_TextChanged(null, null);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) {
            LoadProcesses();
            ApplySearchFilter();
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            string query = SearchBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(query)) {
                ProcessListView.ItemsSource = _allProcesses;
            } else {
                var filtered = _allProcesses.Where(p =>
                    (!string.IsNullOrEmpty(p.Name) && p.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                    p.Id.ToString().Contains(query) ||
                    (!string.IsNullOrEmpty(p.Path) && p.Path.Contains(query, StringComparison.OrdinalIgnoreCase))
                ).ToList();
                ProcessListView.ItemsSource = filtered;
            }
        }

        private void ProcessListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            OkButton.IsEnabled = ProcessListView.SelectedItems.Count > 0;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            this.SelectedProcesses = [.. ProcessListView.SelectedItems
                .OfType<ProcessInfo>()
                .Select(p => p.Name)];
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = false;
            Close();
        }
    }
}
