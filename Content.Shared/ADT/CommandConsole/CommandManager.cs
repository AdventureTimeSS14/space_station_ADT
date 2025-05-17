using System;
using System.Collections.Generic;

namespace Content.Shared.ADT.CommandConsole
{
    public sealed class CommandManager
    {
        private readonly Dictionary<string, Func<string[], string>> _commands = new();

        public CommandManager()
        {
            // Зарегистрируем несколько базовых команд
            RegisterCommand("echo", Echo);
            RegisterCommand("help", Help);
            // Добавь свои команды тут
        }

        public void RegisterCommand(string name, Func<string[], string> handler)
        {
            _commands[name] = handler;
        }

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
                    return func.Invoke(args);
                }
                catch (Exception e)
                {
                    return $"Error executing command '{cmd}': {e.Message}";
                }
            }
            else
            {
                return $"Unknown command: {cmd}. Type 'help' for list.";
            }
        }

        private string Echo(string[] args)
        {
            return string.Join(' ', args);
        }

        private string Help(string[] args)
        {
            var available = string.Join(", ", _commands.Keys);
            return "Available commands: " + available;
        }
    }
}
