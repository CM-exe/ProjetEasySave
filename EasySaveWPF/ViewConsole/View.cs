using ProjetEasySave.Model;

namespace ProjetEasySave.View
{
    /// <summary>
    /// Represents the console-based User Interface for the EasySave application.
    /// Handles command-line arguments, displays the interactive menu, captures user input, and communicates with the <see cref="ViewModel"/>.
    /// </summary>
    public class View
    {
        private readonly ViewModel.ViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="View"/> class and sets up the underlying ViewModel.
        /// </summary>
        public View()
        {
            _viewModel = new ViewModel.ViewModel();
        }

        /// <summary>
        /// Starts the view logic. If command-line arguments are provided, it parses and executes them directly.
        /// Otherwise, it launches the interactive console menu.
        /// </summary>
        /// <param name="args">An array of command-line arguments passed to the application.</param>
        public async void run(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            bool running = true;

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
                        if (int.TryParse(start_str, out int start) && int.TryParse(to_str, out int to)) {
                            if (start > to) {
                                //Console.WriteLine($"{View.ERROR}: Invalid SaveSpace ID. Abort.");
                                renderMessage(_viewModel.translate("InvalidSaveSpaceID"), ConsoleColor.Red);
                            }
                            else
                            {
                                // Valid SaveSpace, from "start" to "to"
                                List<SaveSpace> spaces = _viewModel.getSaveSpaces();
                                start--;
                                to--;
                                if(spaces.Count < to) {
                                    renderMessage(_viewModel.translate("InvalidSaveSpaceID"), ConsoleColor.Red);
                                }
                                for (int i = 0; i <= to; i++)
                                {
                                    var ok = await _viewModel.startSave(spaces[i].getName());
                                    renderResult(ok, _viewModel.translate("SaveStarted"), _viewModel.translate("SaveStartFailed"));
                                    while (spaces[i].getTaskStates().Contains(SaveTaskState.RUNNING))
                                    {
                                        await Task.Delay(100);
                                    }
                                    renderResult(ok, _viewModel.translate("SaveCompleted"), _viewModel.translate("SaveFailed"));
                                }
                            }
                        }
                        else
                        {
                            renderMessage(_viewModel.translate("InvalidSaveSpaceID"), ConsoleColor.Red);
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
                            }
                            else
                            {
                                List<SaveSpace> spaces = _viewModel.getSaveSpaces();
                                start--;
                                to--;
                                if (spaces.Count < to)
                                {
                                    renderMessage(_viewModel.translate("InvalidSaveSpaceID"), ConsoleColor.Red);
                                }
                                // Start save for "start"
                                var ok = await _viewModel.startSave(spaces[start].getName());
                                renderResult(ok, _viewModel.translate("SaveStarted"), _viewModel.translate("SaveStartFailed"));
                                while (spaces[start].getTaskStates().Contains(SaveTaskState.RUNNING))
                                {
                                    Thread.Sleep(100);
                                }
                                renderResult(ok, _viewModel.translate("SaveCompleted"), _viewModel.translate("SaveFailed"));

                                if (start != to)
                                {
                                    // Start save for "to"
                                    var ok_ = await _viewModel.startSave(spaces[to].getName());
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
                        }
                    } else
                    {
                        // Invalid argument
                        renderMessage(_viewModel.translate("UsageCommandExemple"), ConsoleColor.White);
                    }
                }
            }
            else {
                while (running)
                {
                    renderHeader();
                    renderMenu();

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("> ");
                    Console.ResetColor();
                    var choice = Console.ReadLine()?.Trim();

                    switch (choice)
                    {
                        case "1":
                            addSaveSpaceFlow();
                            break;
                        case "2":
                            removeSaveSpaceFlow();
                            break;
                        case "3":
                            startSaveFlow();
                            break;
                        case "4":
                            listSaveSpacesFlow();
                            break;
                        case "5":
                            changeLanguageFlow();
                            break;
                        case "6":
                            viewSaveSpacesStateFlow();
                            break;
                        case "7":
                            changeLogFormatFlow();
                            break;
                        case "0":
                            running = false;
                            break;
                        default:
                            renderMessage(_viewModel.translate("InvalidChoice"), ConsoleColor.Yellow);
                            pause();
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Clears the console and displays the application header.
        /// </summary>
        private void renderHeader()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=== EasySave ===");
            Console.ResetColor();
        }

        /// <summary>
        /// Displays the interactive main menu options to the user.
        /// </summary>
        private void renderMenu()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("1) " + _viewModel.translate("AddSaveSpace"));
            Console.WriteLine("2) " + _viewModel.translate("RemoveSaveSpace"));
            Console.WriteLine("3) " + _viewModel.translate("StartSave"));
            Console.WriteLine("4) " + _viewModel.translate("ListSaveSpaces"));
            Console.WriteLine("5) " + _viewModel.translate("ChangeLanguage"));
            Console.WriteLine("6) " + _viewModel.translate("SaveSpacesState"));
            Console.WriteLine("7) " + _viewModel.translate("ChangeLogFormat"));
            Console.WriteLine("0) " + _viewModel.translate("Exit"));
            Console.ResetColor();
            Console.WriteLine();

        }

        /// <summary>
        /// Handles the interactive flow for creating and adding a new backup job (Save Space).
        /// </summary>
        private void addSaveSpaceFlow()
        {
            Console.Write(_viewModel.translate("Name") + ": ");
            var name = Console.ReadLine() ?? string.Empty;

            Console.Write(_viewModel.translate("SourcePath") + ": ");
            var source = Console.ReadLine() ?? string.Empty;

            Console.Write(_viewModel.translate("DestinationPath") + ": ");
            var destination = Console.ReadLine() ?? string.Empty;

            Console.Write(_viewModel.translate("SaveType") + " (complete/differential): ");
            var typeSave = Console.ReadLine() ?? string.Empty;

            var completeSavePath = string.Empty;
            if (typeSave.ToLower() == "differential") {
                Console.Write(_viewModel.translate("CompleteSavePath") + ": ");
                completeSavePath = Console.ReadLine() ?? string.Empty;
            }
            else
            {
                completeSavePath = "";
            }

            var ok = _viewModel.addSaveSpace(name, source, destination, typeSave, [], completeSavePath);
            renderResult(ok, _viewModel.translate("SaveSpaceAdded"), _viewModel.translate("SaveSpaceAddFailed"));
            pause();
        }

        /// <summary>
        /// Handles the interactive flow for removing an existing backup job.
        /// </summary>
        private void removeSaveSpaceFlow()
        {
            Console.Write(_viewModel.translate("Name") + ": ");
            var name = Console.ReadLine() ?? string.Empty;

            var ok = _viewModel.removeSaveSpace(name);
            renderResult(ok, _viewModel.translate("SaveSpaceRemoved"), _viewModel.translate("SaveSpaceRemoveFailed"));
            pause();
        }

        /// <summary>
        /// Handles the interactive flow for manually starting a specific backup job.
        /// </summary>
        private async void startSaveFlow()
        {
            Console.Write(_viewModel.translate("Name") + ": ");
            var name = Console.ReadLine() ?? string.Empty;

            var ok = await _viewModel.startSave(name);
            renderResult(ok, _viewModel.translate("SaveStarted"), _viewModel.translate("SaveStartFailed"));
            pause();
        }

        /// <summary>
        /// Displays a list of all currently configured backup jobs and their details.
        /// </summary>
        private void listSaveSpacesFlow()
        {
            List<SaveSpace> spaces = _viewModel.getSaveSpaces();

            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(_viewModel.translate("SaveSpacesTitle"));
            Console.ResetColor();

            if (spaces == null || spaces.Count == 0)
            {
                renderMessage(_viewModel.translate("NoSaveSpaces"), ConsoleColor.DarkYellow);
            }
            else
            {
                foreach (var space in spaces)
                {
                    if (space.getTypeSave() == "differential") {
                         Console.WriteLine($"- {space.getName()} | {space.getSourcePath()} -> {space.getDestinationPath()} (Complete save : {space.getCompleteSavePath()}) | {space.getTypeSave()}");
                    }
                    else
                    {
                        Console.WriteLine($"- {space.getName()} | {space.getSourcePath()} -> {space.getDestinationPath()} | {space.getTypeSave()}");
                    }
                        
                }
            }

            pause();
        }

        /// <summary>
        /// Displays the real-time execution state of all configured backup jobs.
        /// Refreshes automatically until the user presses Enter.
        /// </summary>
        private void viewSaveSpacesStateFlow()
        {
            while (true)
            {
                renderHeader();
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(_viewModel.translate("SaveSpacesTitle"));
                Console.ResetColor();

                List<SaveSpace> spaces = _viewModel.getSaveSpaces();
                if (spaces == null || spaces.Count == 0)
                {
                    renderMessage(_viewModel.translate("NoSaveSpaces"), ConsoleColor.DarkYellow);
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
                Console.WriteLine(_viewModel.translate("PressEnter"));
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

        /// <summary>
        /// Handles the interactive flow for changing the application's display language.
        /// </summary>
        private void changeLanguageFlow()
        {
            Console.Write(_viewModel.translate("LanguageCodePrompt") + ": ");
            var code = Console.ReadLine() ?? string.Empty;

            _viewModel.setLanguage(code);
            renderMessage(_viewModel.translate("Language") + ": " + _viewModel.getLanguage(), ConsoleColor.Green);
            pause();
        }

        /// <summary>
        /// Handles the interactive flow for changing the format of the generated log files (e.g., JSON or XML).
        /// </summary>
        private void changeLogFormatFlow()
        {
            renderHeader();

            // Use the translated prompt from translations.json
            Console.Write(_viewModel.translate("LogFormatPrompt"));
            string choice = Console.ReadLine()?.Trim().ToUpper();

            bool success = _viewModel.setLogsFormat(choice);

            // Results are now also translated
            renderResult(
                success,
                _viewModel.translate("LogFormatUpdated"),
                _viewModel.translate("InvalidLogFormat")
            );

            pause();
        }

        /// <summary>
        /// Helper method to display a success or failure message based on a boolean condition.
        /// </summary>
        /// <param name="ok">The boolean condition determining which message to show.</param>
        /// <param name="success">The message to display if true.</param>
        /// <param name="fail">The message to display if false.</param>
        private static void renderResult(bool ok, string success, string fail)
        {
            renderMessage(ok ? success : fail, ok ? ConsoleColor.Green : ConsoleColor.Red);
        }

        /// <summary>
        /// Helper method to print a colored message to the console.
        /// </summary>
        /// <param name="message">The text string to display.</param>
        /// <param name="color">The <see cref="ConsoleColor"/> to apply to the text.</param>
        private static void renderMessage(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        /// <summary>
        /// Pauses the application flow and waits for the user to press the Enter key before continuing.
        /// </summary>
        private void pause()
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine();
            Console.Write(_viewModel.translate("PressEnterPause"));
            Console.ResetColor();
            Console.ReadLine();
        }
    }
}
