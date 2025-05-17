using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.Shared.ADT.CommandConsole
{
    public sealed class CommandManager
    {
        private readonly Dictionary<string, Func<string[], string>> _commands = new();

        private Directory _rootDirectory = new() { Name = "" };
        private Directory? _currentDirectory;
        private string _currentPath = "/";

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
        }

        public void RegisterCommand(string name, Func<string[], string> handler)
        {
            _commands[name] = handler;
        }

        public bool ExitRequested { get; private set; } = false;

        public string CurrentPath => _currentPath; // Публичное свойство

        public string Execute(string input)
        {
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return "";

            var cmd = parts[0];
            var args = parts.Skip(1).ToArray();

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
