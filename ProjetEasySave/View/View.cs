using System;
using System.Collections.Generic;
using System.Threading;
using ProjetEasySave.Model;
using ProjetEasySave.ViewModel;

namespace ProjetEasySave.View
{
    public class View
    {
        private readonly ViewModel.ViewModel _viewModel;

        public View()
        {
            _viewModel = new ViewModel.ViewModel();
        }

        public void Run()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            bool running = true;

            while (running)
            {
                RenderHeader();
                RenderMenu();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("> ");
                Console.ResetColor();
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        AddSaveSpaceFlow();
                        break;
                    case "2":
                        RemoveSaveSpaceFlow();
                        break;
                    case "3":
                        StartSaveFlow();
                        break;
                    case "4":
                        ListSaveSpacesFlow();
                        break;
                    case "5":
                        ChangeLanguageFlow();
                        break;
                    case "6":
                        ViewSaveSpacesStateFlow();
                        break;
                    case "0":
                        running = false;
                        break;
                    default:
                        RenderMessage(_viewModel.Translate("InvalidChoice"), ConsoleColor.Yellow);
                        Pause();
                        break;
                }
            }
        }

        private void RenderHeader()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=== EasySave ===");
            Console.ResetColor();
        }

        private void RenderMenu()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("1) " + _viewModel.Translate("AddSaveSpace"));
            Console.WriteLine("2) " + _viewModel.Translate("RemoveSaveSpace"));
            Console.WriteLine("3) " + _viewModel.Translate("StartSave"));
            Console.WriteLine("4) " + _viewModel.Translate("ListSaveSpaces"));
            Console.WriteLine("5) " + _viewModel.Translate("ChangeLanguage"));
            Console.WriteLine("6) " + _viewModel.Translate("SaveSpacesState"));
            Console.WriteLine("0) " + _viewModel.Translate("Exit"));
            Console.ResetColor();
            Console.WriteLine();
        }

        private void AddSaveSpaceFlow()
        {
            Console.Write(_viewModel.Translate("Name") + ": ");
            var name = Console.ReadLine() ?? string.Empty;

            Console.Write(_viewModel.Translate("SourcePath") + ": ");
            var source = Console.ReadLine() ?? string.Empty;

            Console.Write(_viewModel.Translate("DestinationPath") + ": ");
            var destination = Console.ReadLine() ?? string.Empty;

            Console.Write(_viewModel.Translate("SaveType") + " (complete/differential): ");
            var typeSave = Console.ReadLine() ?? string.Empty;

            var ok = _viewModel.AddSaveSpace(name, source, destination, typeSave);
            RenderResult(ok, _viewModel.Translate("SaveSpaceAdded"), _viewModel.Translate("SaveSpaceAddFailed"));
            Pause();
        }

        private void RemoveSaveSpaceFlow()
        {
            Console.Write(_viewModel.Translate("Name") + ": ");
            var name = Console.ReadLine() ?? string.Empty;

            var ok = _viewModel.RemoveSaveSpace(name);
            RenderResult(ok, _viewModel.Translate("SaveSpaceRemoved"), _viewModel.Translate("SaveSpaceRemoveFailed"));
            Pause();
        }

        private void StartSaveFlow()
        {
            Console.Write(_viewModel.Translate("Name") + ": ");
            var name = Console.ReadLine() ?? string.Empty;

            var ok = _viewModel.StartSave(name);
            RenderResult(ok, _viewModel.Translate("SaveStarted"), _viewModel.Translate("SaveStartFailed"));
            Pause();
        }

        private void ListSaveSpacesFlow()
        {
            List<SaveSpace> spaces = _viewModel.GetSaveSpaces();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(_viewModel.Translate("SaveSpacesTitle"));
            Console.ResetColor();

            if (spaces == null || spaces.Count == 0)
            {
                RenderMessage(_viewModel.Translate("NoSaveSpaces"), ConsoleColor.DarkYellow);
            }
            else
            {
                foreach (var space in spaces)
                {
                    Console.WriteLine($"- {space.getName()} | {space.getSourcePath()} -> {space.getDestinationPath()} | {space.getTypeSave()}");
                }
            }

            Pause();
        }

        private void ViewSaveSpacesStateFlow()
        {
            while (true)
            {
                RenderHeader();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(_viewModel.Translate("SaveSpacesTitle"));
                Console.ResetColor();

                List<SaveSpace> spaces = _viewModel.GetSaveSpaces();
                if (spaces == null || spaces.Count == 0)
                {
                    RenderMessage(_viewModel.Translate("NoSaveSpaces"), ConsoleColor.DarkYellow);
                }
                else
                {
                    foreach (var space in spaces)
                    {
                        Console.WriteLine($"- {space.getName()} | {string.Join(", ", space.getTaskStates())}");
                    }
                }

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(_viewModel.Translate("PressEnter"));
                Console.ResetColor();

                var start = DateTime.UtcNow;
                while ((DateTime.UtcNow - start).TotalMilliseconds < 1000)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Enter)
                        {
                            return;
                        }
                    }

                    Thread.Sleep(50);
                }
            }
        }

        private void ChangeLanguageFlow()
        {
            Console.Write(_viewModel.Translate("LanguageCodePrompt") + ": ");
            var code = Console.ReadLine() ?? string.Empty;

            _viewModel.SetLanguage(code);
            RenderMessage(_viewModel.Translate("Language") + ": " + _viewModel.GetLanguage(), ConsoleColor.Green);
            Pause();
        }

        private static void RenderResult(bool ok, string success, string fail)
        {
            RenderMessage(ok ? success : fail, ok ? ConsoleColor.Green : ConsoleColor.Red);
        }

        private static void RenderMessage(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private void Pause()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine();
            Console.Write(_viewModel.Translate("PressEnterPause"));
            Console.ResetColor();
            Console.ReadLine();
        }
    }
}
