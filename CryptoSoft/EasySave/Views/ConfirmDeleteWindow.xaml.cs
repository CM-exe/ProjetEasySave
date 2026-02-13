using System.Windows;

namespace EasySave.Views {
    /// <summary>
    /// Interaction logic for ConfirmDeleteWindow.xaml
    /// </summary>
    public partial class ConfirmDeleteWindow : Window {
        // Property to indicate whether the deletion is confirmed
        public bool IsConfirmed { get; private set; } = false;

        // Constructor for the ConfirmDeleteWindow
        public ConfirmDeleteWindow() {
            // Initialize the window components
            InitializeComponent();
        }

        // Event handler for the Yes button click
        private void Yes_Click(object sender, RoutedEventArgs e) {
            // Set the IsConfirmed property to true and close the window
            IsConfirmed = true;
            this.DialogResult = true;
            // close the window
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e) {
            // Set the IsConfirmed property to false and close the window
            this.DialogResult = false;
            // close the window
            Close();
        }
    }
}
