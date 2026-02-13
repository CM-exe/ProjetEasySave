using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using EasySave.Model;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace EasySave.Views {
    partial class Configuration : System.Windows.Controls.UserControl {
        private readonly IViewModel _ViewModel;
        public Configuration(IViewModel viewModel) {
            InitializeComponent();
            this._ViewModel = viewModel;
            this.MainGrid.DataContext = _ViewModel;
        }

        private void StateFileClick(object sender, EventArgs e) {
            var dialog = new Microsoft.Win32.OpenFileDialog {
                Filter = "Fichiers JSON (*.json)|*.json",
                InitialDirectory = Directory.GetCurrentDirectory()
            };

            if (dialog.ShowDialog() == true) {
                string selectedFilePath = dialog.FileName;
                this._ViewModel.StateFile = selectedFilePath;
            }
        }

        private void LogFileClick(object sender, EventArgs e) {
            var dialog = new Microsoft.Win32.OpenFileDialog {
                Filter = "Fichiers JSON (*.json)|*.json|Fichiers XML (*.xml)|*.xml",
                InitialDirectory = Directory.GetCurrentDirectory()
            };

            if (dialog.ShowDialog() == true) {
                string selectedFilePath = dialog.FileName;
                this._ViewModel.LogFile = selectedFilePath;

            }
        }

        private void CryptoFileClick(object sender, EventArgs e) {
            var dialog = new Microsoft.Win32.OpenFileDialog {
                Filter = "Fichiers exe (*.exe)|*.exe",
                InitialDirectory = Directory.GetCurrentDirectory()
            };

            if (dialog.ShowDialog() == true) {
                string selectedFilePath = dialog.FileName;
                this._ViewModel.CryptoFile = selectedFilePath;

            }
        }

        private void AddExtension_Click(object sender, RoutedEventArgs e) {
            var input = ExtensionInput.Text?.Trim();
            if (string.IsNullOrEmpty(input))
                return;

            if (_ViewModel?.Configuration?.CryptoExtensions != null && !_ViewModel.Configuration.CryptoExtensions.Contains(input)) {
                _ViewModel.Configuration.CryptoExtensions.Add(input);
                ExtensionInput.Clear();
            }
        }

        private void RemoveExtensionItem_Click(object sender, RoutedEventArgs e) {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string item) {
                _ViewModel?.Configuration?.CryptoExtensions?.Remove(item);
            }
        }

        private void AddProcess_Click(object sender, RoutedEventArgs e) {
            var input = ProcessInput.Text?.Trim();
            if (string.IsNullOrEmpty(input))
                return;

            if (_ViewModel?.Configuration?.Processes != null && !_ViewModel.Configuration.Processes.Contains(input)) {
                _ViewModel.Configuration.Processes.Add(input);
                ProcessInput.Clear();
            }
        }

        private void RemoveProcessItem_Click(object sender, RoutedEventArgs e) {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string item) {
                _ViewModel?.Configuration?.Processes?.Remove(item);
            }
        }

        private void ClearExtensions_Click(object sender, RoutedEventArgs e) {
            _ViewModel?.Configuration?.CryptoExtensions?.Clear();
        }

        private void ClearProcesses_Click(object sender, RoutedEventArgs e) {
            _ViewModel?.Configuration?.Processes?.Clear();
        }

        private void SelectProcess_Click(object sender, RoutedEventArgs e) {
            var dlg = new SelectProcess(this._ViewModel) {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() == true && dlg.SelectedProcesses.Count > 0) {
                foreach (var processName in dlg.SelectedProcesses) {
                    if (_ViewModel?.Configuration?.Processes != null && !_ViewModel.Configuration.Processes.Contains(processName)) {
                        _ViewModel.Configuration.Processes.Add(processName);
                    }
                }
            }
        }

        private void GenerateEncryptionKey_Click(object sender, RoutedEventArgs e) {
            // Generate a 64-character (256-bit) random hex key
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(bytes);
            }
            var hex = BitConverter.ToString(bytes).Replace("-", "");
            // Set the key in the ViewModel/Configuration
            _ViewModel.Configuration.CryptoKey = hex;
            _ViewModel.OnPropertyChanged("EncryptionKey");
        }

        // --- PriorityExtensions Handlers ---

        private void AddPriorityExtension_Click(object sender, RoutedEventArgs e) {
            var input = PriorityExtensionInput.Text?.Trim();
            if (string.IsNullOrEmpty(input))
                return;

            if (_ViewModel?.Configuration?.PriorityExtensions != null && !_ViewModel.Configuration.PriorityExtensions.Contains(input)) {
                _ViewModel.Configuration.PriorityExtensions.Add(input);
                PriorityExtensionInput.Clear();
            }
        }

        private void RemovePriorityExtensionItem_Click(object sender, RoutedEventArgs e) {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string item) {
                _ViewModel?.Configuration?.PriorityExtensions?.Remove(item);
            }
        }

        private void ClearPriorityExtensions_Click(object sender, RoutedEventArgs e) {
            _ViewModel?.Configuration?.PriorityExtensions?.Clear();
        }

        // --- Numeric Only Input for MaxConcurrentJobs/MaxConcurrentSize ---

        private void NumericOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }
    }
}