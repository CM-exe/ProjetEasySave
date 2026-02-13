using EasySave.Model;
using System.Data;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;

namespace EasySave;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application {
    private readonly IViewModel _ViewModel;

    public App() {
        this._ViewModel = new ViewModel();
    }

    public string RunCommandList() {
        if (this._ViewModel is null) {
            throw new Exception("ViewModel is not initialized.");
        }

        string result = string.Empty;
        for (int i = 0; i < this._ViewModel.Configuration.Jobs.Count; i++) {
            IBackupJobConfiguration job = this._ViewModel.Configuration.Jobs[i];
            string prefix = new(' ', ((string.Empty + (i + 1))).Length);
            result += $"{i + 1}. {Language.Instance.Translations["JOB_NAME"]}: {job.Name}\n" +
                      $"{prefix}. {Language.Instance.Translations["JOB_SOURCE"]}: {job.Source}\n" +
                      $"{prefix}. {Language.Instance.Translations["JOB_DESTINATION"]}: {job.Destination}\n" +
                      $"{prefix}. {Language.Instance.Translations["JOB_TYPE"]}: {job.Type}\n\n";
        }

        return result;
    }

    public string RunCommandConfiguration() {
        var jsonObject = Configuration.Instance?.ToJSON() ?? [];

        string jsonString = jsonObject.ToJsonString(new JsonSerializerOptions {
            WriteIndented = true
        });

        return jsonString;
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool AllocConsole();
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool AttachConsole(int dwProcessId);
    private const int ATTACH_PARENT_PROCESS = -1;

    public void ApplicationStartup(object sender, StartupEventArgs e) {
        string[] args = e.Args;

        if (args.Length > 0) {
            App.AttachConsole(App.ATTACH_PARENT_PROCESS);
            this._ViewModel.LanguageChanged += this.OnLanguageChanged;
            this._ViewModel.JobStateChanged += this.OnJobStateChanged;
            this._ViewModel.ConfigurationChanged += this.OnConfigurationChanged;

            this._ViewModel.Commands.RegisterCommand("list", (command) => this.RunCommandList());
            this._ViewModel.Commands.RegisterCommand("configuration", (command) => this.RunCommandConfiguration());

            this._ViewModel.Commands.RunCommand(string.Join(" ", args));

            this.Shutdown();
        } else {
            // DEV
            //App.AllocConsole();
            MainWindow mainWindow = new(this._ViewModel);
            mainWindow.Show();
        }
    }

    public void OnLanguageChanged(object sender, LanguageChangedEventArgs e) {
        Console.WriteLine(Language.Instance.Translations["LANGUAGE_CHANGED"] + ": " + e.Language);
    }

    public void OnJobStateChanged(object sender, JobStateChangedEventArgs e) {
        switch (e.JobState?.State) {
            case State.ACTIVE:
                Console.WriteLine(Language.Instance.Translations["JOB_STATE_STARTED"] + " : " + e.JobState.BackupJob.Name);
                break;
            case State.IN_PROGRESS:
                Console.WriteLine(
                    Language.Instance.Translations["JOB_STATE_IN_PROGRESS"] +
                    " : " + e.JobState.BackupJob.Name + " => " + e.JobState.Progression + "% "
                );
                break;
            case State.END:
                Console.WriteLine(Language.Instance.Translations["JOB_STATE_ENDED"] + " : " + e.JobState.BackupJob.Name);
                break;
        }
    }

    public void OnConfigurationChanged(object sender, ConfigurationChangedEventArgs e) {
        Console.WriteLine(Language.Instance.Translations["CONFIGURATION_CHANGED"]);
        foreach (IBackupJobConfiguration job in this._ViewModel.Configuration.Jobs) {
            Console.WriteLine(Language.Instance.Translations["JOB_NAME"] + ": " + job.Name +
                 ", " + Language.Instance.Translations["JOB_SOURCE"] + ": " + job.Source +
                 ", " + Language.Instance.Translations["JOB_DESTINATION"] + ": " + job.Destination +
                 ", " + Language.Instance.Translations["JOB_TYPE"] + ": " + job.Type);
        }
    }
}

