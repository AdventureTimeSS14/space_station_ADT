using System;
using System.Collections.Generic;

namespace Content.Shared.ADT.CommandConsole
{
    public sealed class CommandManager
    {
        private readonly Dictionary<string, Func<string[], string>> _commands = new();

        public CommandManager()
        {
            RegisterCommand("echo", Echo);
            RegisterCommand("help", Help);
            RegisterCommand("clear", Clear);
            RegisterCommand("time", Time);
            RegisterCommand("exit", Exit);
            // ....
        }

        public void RegisterCommand(string name, Func<string[], string> handler)
        {
            _commands[name] = handler;
        }

        public bool ExitRequested { get; private set; } = false;

        public string Execute(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();
            var args = parts.Length > 1 ? parts[1..] : Array.Empty<string>();

            if (_commands.TryGetValue(cmd, out var func))
            {
                try
                {
                    var result = func.Invoke(args);
                    if (cmd == "exit" || cmd == "quit")
                        ExitRequested = true;
                    return result;
                }
                catch (Exception e)
                {
                    return $"[Error] executing '{cmd}': {e.Message}";
                }
            }
            else
            {
                return $"Unknown command: '{cmd}'. Type 'help' to list commands.";
            }
        }

        private string Echo(string[] args) => string.Join(' ', args);

        private string Help(string[] args)
        {
            return "Available commands: " + string.Join(", ", _commands.Keys);
        }

        private string Clear(string[] args)
        {
            // возвращаем пустую строку, чтобы не было nullable
            return "";
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
