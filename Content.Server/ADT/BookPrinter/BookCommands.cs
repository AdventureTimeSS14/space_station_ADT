using System.Linq;
using Content.Server.Administration;
using Content.Server.Database;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.ADT.BookPrinter.Commands
{
    [AdminCommand(AdminFlags.Server)]
    public sealed class DeleteBookCommand : LocalizedCommands
    {
        [Dependency] private readonly IServerDbManager _db = default!;

        public override string Command => "deletebook";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 1)
            {
                shell.WriteError("Использование: deletebook <ID>");
                shell.WriteError("Пример: deletebook 15");
                return;
            }

            if (!int.TryParse(args[0], out var bookId))
            {
                shell.WriteError("ID книги должен быть числом!");
                return;
            }

            DeleteBookAsync(shell, bookId);
        }

        private async void DeleteBookAsync(IConsoleShell shell, int bookId)
        {
            try
            {
                var success = await _db.DeleteBookPrinterEntryAsync(bookId);

                if (success)
                {
                    shell.WriteLine($"Книга с ID {bookId} успешно удалена из базы данных.");

                    var bookPrinterSystem = IoCManager.Resolve<IEntitySystemManager>()
                        .GetEntitySystem<BookPrinterSystem>();
                    bookPrinterSystem.RefreshBookContent();
                }
                else
                {
                    shell.WriteError($"Книга с ID {bookId} не найдена в базе данных.");
                }
            }
            catch (Exception ex)
            {
                shell.WriteError($"Ошибка при удалении книги: {ex.Message}");
            }
        }

        public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                return CompletionResult.FromHintOptions(["<ID книги>"], "ID книги для удаления");
            }

            return CompletionResult.Empty;
        }
    }

    [AdminCommand(AdminFlags.Server)]
    public sealed class ListBooksCommand : LocalizedCommands
    {
        [Dependency] private readonly IServerDbManager _db = default!;

        public override string Command => "listbooks";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            ListBooksAsync(shell);
        }

        private async void ListBooksAsync(IConsoleShell shell)
        {
            try
            {
                var books = await _db.GetBookPrinterEntriesAsync();

                if (!books.Any())
                {
                    shell.WriteLine("База данных книг пуста.");
                    return;
                }

                shell.WriteLine($"Найдено книг в базе: {books.Count()}");
                shell.WriteLine("=====================================");

                foreach (var book in books.OrderBy(b => b.Id))
                {
                    shell.WriteLine($"ID: {book.Id}");
                    shell.WriteLine($"Название: {book.Name}");
                    shell.WriteLine($"Описание: {book.Description}");
                    shell.WriteLine($"Размер: {book.Content.Length} символов");
                    shell.WriteLine("-------------------------------------");
                }
            }
            catch (Exception ex)
            {
                shell.WriteError($"Ошибка при получении списка книг: {ex.Message}");
            }
        }
    }
}
