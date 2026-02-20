using ProjetEasySave.Model;
using ProjetEasySave.ViewModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace EasySaveWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ViewModel _viewModel = new ViewModel();

            if (e.Args.Length == 0)
            {
                // No command line arguments, so run as normal Windows application.
                var mainWindow = new MainWindow();
                mainWindow.ShowDialog();
            }
            else
            {
                string[] args = e.Args;
                // Parse arguments
                string args_str = String.Join(" ", args);
                if (args.Length > 0)
                {
                    foreach (string key in args)
                    {
                        if (key.Contains("-"))
                        {
                            string start_str = args_str.Substring(0, args_str.IndexOf('-'));
                            string to_str = args_str.Substring(args_str.IndexOf('-') + 1);
                            // Check if valid integers
                            if (int.TryParse(start_str, out int start) && int.TryParse(to_str, out int to))
                            {
                                if (start > to)
                                {
                                    //Console.WriteLine($"{View.ERROR}: Invalid SaveSpace ID. Abort.");
                                    renderMessage(_viewModel.translate("InvalidSaveSpaceID"), ConsoleColor.Red);
                                    Shutdown();
                                    return;
                                }
                                else
                                {
                                    // Valid SaveSpace, from "start" to "to"
                                    List<SaveSpace> spaces = _viewModel.getSaveSpaces();
                                    start--;
                                    to--;
                                    if (spaces.Count < to)
                                    {
                                        renderMessage(_viewModel.translate("InvalidSaveSpaceID"), ConsoleColor.Red);
                                        Shutdown();
                                        return;
                                    }

                                    if (spaces.Count == 0)
                                    {
                                        renderMessage(_viewModel.translate("NoSaveSpaces"), ConsoleColor.Red);
                                        Shutdown();
                                        return;
                                    }
                                    for (int i = 0; i <= to; i++)
                                    {
                                        var ok = _viewModel.startSave(spaces[i].getName());
                                        renderResult(ok, _viewModel.translate("SaveStarted"), _viewModel.translate("SaveStartFailed"));
                                        while (spaces[i].getTaskStates().Contains(SaveTaskState.RUNNING))
                                        {
                                            Thread.Sleep(100);
                                        }
                                        renderResult(ok, _viewModel.translate("SaveCompleted"), _viewModel.translate("SaveFailed"));
                                    }
                                }
                            }
                            else
                            {
                                renderMessage(_viewModel.translate("InvalidSaveSpaceID"), ConsoleColor.Red);
                                Shutdown();
                                return;
                            }
                        }
                        else if (key.Contains(";"))
                        {
                            string start_str = args_str.Substring(0, args_str.IndexOf(';'));
                            string to_str = args_str.Substring(args_str.IndexOf(';') + 1);
                            // Check if valid integers
                            if (int.TryParse(start_str, out int start) && int.TryParse(to_str, out int to))
                            {
                                if (start > to)
                                {
                                    renderMessage(_viewModel.translate("InvalidSaveSpaceID"), ConsoleColor.Red);
                                    Shutdown();
                                    return;
                                }
                                else
                                {
                                    List<SaveSpace> spaces = _viewModel.getSaveSpaces();
                                    start--;
                                    to--;
                                    if (spaces.Count < to)
                                    {
                                        renderMessage(_viewModel.translate("InvalidSaveSpaceID"), ConsoleColor.Red);
                                        Shutdown();
                                        return;
                                    }
                                    if(spaces.Count == 0)
                                    {
                                        renderMessage(_viewModel.translate("NoSaveSpaces"), ConsoleColor.Red);
                                        Shutdown();
                                        return;
                                    }
                                    // Start save for "start"
                                    var ok = _viewModel.startSave(spaces[start].getName());
                                    renderResult(ok, _viewModel.translate("SaveStarted"), _viewModel.translate("SaveStartFailed"));
                                    while (spaces[start].getTaskStates().Contains(SaveTaskState.RUNNING))
                                    {
                                        Thread.Sleep(100);
                                    }
                                    renderResult(ok, _viewModel.translate("SaveCompleted"), _viewModel.translate("SaveFailed"));

                                    if (start != to)
                                    {
                                        // Start save for "to"
                                        var ok_ = _viewModel.startSave(spaces[to].getName());
                                        renderResult(ok_, _viewModel.translate("SaveStarted"), _viewModel.translate("SaveStartFailed"));
                                        while (spaces[to].getTaskStates().Contains(SaveTaskState.RUNNING))
                                        {
                                            Thread.Sleep(100);
                                        }
                                        renderResult(ok_, _viewModel.translate("SaveCompleted"), _viewModel.translate("SaveFailed"));
                                    }
                                }
                            }
                            else
                            {
                                renderMessage(_viewModel.translate("InvalidSaveSpaceID"), ConsoleColor.Red);
                                Shutdown();
                                return;
                            }
                        }
                        else
                        {
                            // Invalid argument
                            renderMessage(_viewModel.translate("UsageCommandExemple"), ConsoleColor.White);
                        }
                    }
                }
                Shutdown();
            }
        }
        private static void renderMessage(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void renderResult(Task<bool> ok, string success, string fail)
        {
            renderMessage(ok.Result ? success : fail, ok.Result ? ConsoleColor.Green : ConsoleColor.Red);
        }
    }

}
