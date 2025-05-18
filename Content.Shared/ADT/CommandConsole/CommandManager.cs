using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Content.Shared.ADT.CommandConsole
{
    public sealed class CommandManager
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        private readonly Dictionary<string, Func<string[], string>> _commands = new();

        private Directory _rootDirectory = new() { Name = "" };
        private Directory? _currentDirectory;
        private string _currentPath = "/";
        private EntityUid? _deviceUid;
        private RogueGameSession? _rogueSession;

        public CommandManager()
        {
            RegisterCommand("echo", Echo);
            RegisterCommand("help", Help);
            RegisterCommand("clear", Clear);
            RegisterCommand("time", Time);
            RegisterCommand("exit", Exit);
            RegisterCommand("ls", Ls);
            RegisterCommand("cd", Cd);
            RegisterCommand("mkdir", Mkdir);
            RegisterCommand("touch", Touch);
            RegisterCommand("cat", Cat);
            RegisterCommand("pwd", Pwd);
            RegisterCommand("rm", Rm);
            RegisterCommand("write", Write);
            RegisterCommand("run", Run);
            RegisterCommand("run_rogue", RunRogue);
        }

        public void RegisterCommand(string name, Func<string[], string> handler)
        {
            _commands[name] = handler;
        }

        public bool ExitRequested { get; private set; } = false;

        public string CurrentPath => _currentPath;

        public string Execute(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "";

            var cmd = parts[0];
            var args = parts.Skip(1).ToArray();

            // Обработка команды run script.sh
            if (cmd == "run" && args.Length > 0)
            {
                var path = args[0];

                if (_currentDirectory == null)
                    return "No current directory.";
                var file = FileSystem.ResolvePathToFile(path, _currentDirectory!);

                if (file == null)
                    return $"File '{path}' not found.";

                if (!file.Name.EndsWith(".sh"))
                    return $"Cannot execute '{file.Name}': not a script file. Please use <name>.sh file.";

                return ExecuteScript(file.Content);
            }


            if (_commands.TryGetValue(cmd, out var handler))
            {
                var result = handler(args);
                if (cmd == "exit")
                {
                    ExitRequested = true;
                }
                return result;
            }
            else
            {
                return $"Unknown command: {cmd}";
            }
        }

        private string ExecuteScript(string scriptContent)
        {
            var result = new StringBuilder();
            var lines = scriptContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                var output = Execute(trimmed);
                if (!string.IsNullOrEmpty(output))
                    result.AppendLine(output);

                if (ExitRequested)
                    break;
            }

            return result.ToString();
        }
        public void SetState(Directory root, string currentPath)
        {
            _rootDirectory = root;
            _currentPath = currentPath;
            _currentDirectory = FindDirectoryByPath(currentPath) ?? _rootDirectory;
        }

        private Directory? FindDirectoryByPath(string path)
        {
            var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            Directory? current = _rootDirectory;

            foreach (var part in parts)
            {
                if (current == null)
                    return null;

                var next = current.Get(part) as Directory;
                if (next == null)
                    return null;

                current = next;
            }

            return current;
        }

        private string Pwd(string[] args) => _currentPath;

        private string Ls(string[] args)
        {
            if (_currentDirectory == null) return "No directory loaded.";
            return string.Join('\n', _currentDirectory.Children.Select(c => c.Name));
        }

        private string Run(string[] args) { return ""; } // Просто заглушка

        private string RunRogue(string[] args)
        {
            if (_rogueSession == null || _rogueSession.IsOver)
            {
                _rogueSession = new RogueGameSession();
                return _rogueSession.Start() + "\nType WASD to move, or 'exit' to quit.";
            }

            if (args.Length == 0)
            {
                return "Game is already running. Enter a move (WASD) or 'exit'.";
            }

            var input = args[0];
            return _rogueSession.ProcessInput(input);
        }

        private string Write(string[] args)
        {
            if (args.Length < 2)
                return "Usage: write <file> <content>";

            if (_currentDirectory == null)
                return "No current directory.";

            var name = args[0];
            var content = string.Join(' ', args.Skip(1));

            var node = _currentDirectory.Get(name);

            if (node is File file)
            {
                file.Content = content;
                return $"File '{name}' updated.";
            }
            else if (node == null)
            {
                var newFile = new File { Name = name, Content = content };
                _currentDirectory.Add(newFile);
                return $"File '{name}' created and written.";
            }
            else
            {
                return $"'{name}' is not a file.";
            }
        }

        private string Cd(string[] args)
        {
            if (args.Length == 0) return "Usage: cd <dir>";

            var target = args[0];
            if (target == "..")
            {
                if (_currentDirectory?.Parent != null)
                {
                    _currentDirectory = _currentDirectory.Parent;
                    _currentPath = _currentDirectory.GetPath();
                }
                return _currentPath;
            }

            if (_currentDirectory == null) return "No current directory.";

            if (_currentDirectory.Get(target) is Directory dir)
            {
                _currentDirectory = dir;
                _currentPath = dir.GetPath();
                return _currentPath;
            }

            return $"Directory '{target}' not found.";
        }

        private string Mkdir(string[] args)
        {
            if (args.Length == 0) return "Usage: mkdir <dir>";
            if (_currentDirectory == null) return "No current directory.";

            var name = args[0];
            if (_currentDirectory.Get(name) != null) return $"'{name}' already exists.";

            _currentDirectory.Add(new Directory { Name = name });
            return $"Directory '{name}' created.";
        }

        private string Rm(string[] args)
        {
            if (args.Length == 0)
                return "Usage: rm [-r] <name>";

            if (_currentDirectory == null)
                return "No current directory.";

            bool recursive = args[0] == "-r";
            string name = recursive ? args.ElementAtOrDefault(1) ?? "" : args[0];

            if (string.IsNullOrWhiteSpace(name))
                return "Usage: rm [-r] <name>";

            var node = _currentDirectory.Get(name);
            if (node == null)
                return $"'{name}' not found.";

            if (node is Directory dir && dir.Children.Any() && !recursive)
                return $"Directory '{name}' is not empty. Use 'rm -r {name}' to remove recursively.";

            _currentDirectory.Children.Remove(node);
            return $"'{name}' deleted.";
        }

        private string Touch(string[] args)
        {
            if (args.Length == 0) return "Usage: touch <file>";
            if (_currentDirectory == null) return "No current directory.";

            var name = args[0];
            if (_currentDirectory.Get(name) != null) return $"'{name}' already exists.";

            _currentDirectory.Add(new File { Name = name, Content = "" });
            return $"File '{name}' created.";
        }

        private string Cat(string[] args)
        {
            if (args.Length == 0) return "Usage: cat <file>";
            if (_currentDirectory == null) return "No current directory.";

            var file = _currentDirectory.Get(args[0]) as File;
            return file != null ? file.Content : $"File '{args[0]}' not found or is not a file.";
        }

        private string Echo(string[] args) => string.Join(' ', args);

        private string Help(string[] args)
        {
            return "Available commands: " + string.Join(", ", _commands.Keys);
        }

        private string Clear(string[] args)
        {
            return ""; // Признак очистки, UI обрабатывает
        }

        private string Time(string[] args)
        {
            return $"Current system time: {DateTime.Now:HH:mm:ss}";
        }

        private string Exit(string[] args)
        {
            return "Exiting command console...";
        }
    }

}
