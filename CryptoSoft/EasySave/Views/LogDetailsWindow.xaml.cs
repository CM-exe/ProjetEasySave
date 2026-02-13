using System.Windows;
using EasySave.Logger;

namespace EasySave.Views;

public partial class LogDetailsWindow : Window {
    public ILog SelectedLog { get; private set; }
    public IViewModel ViewModel { get; private set; }
    public LogDetailsWindow(Log selectedLog, IViewModel viewModel) {
        this.SelectedLog = selectedLog;
        this.ViewModel = viewModel;
        InitializeComponent();
        this.DataContext = this;
    }

    private void Ok_Click(object sender, RoutedEventArgs e) {
        this.Close();
    }
}

