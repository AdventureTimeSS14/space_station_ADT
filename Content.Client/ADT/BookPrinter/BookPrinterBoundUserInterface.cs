// Оригинал данного файла был сделан @temporaldarkness (discord). Код был взят с https://github.com/ss14-ganimed/ENT14-Master.

using Content.Shared.ADT.BookPrinter;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;

namespace Content.Client.ADT.BookPrinter
{
    [UsedImplicitly]
    public sealed class BookPrinterBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private BookPrinterWindow? _window;

        [ViewVariables]
        private BookPrinterBoundUserInterfaceState? _lastState;

        public BookPrinterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new()
            {
                Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName,
            };

            _window.EjectButton.OnPressed += _ => SendMessage(new ItemSlotButtonPressedEvent("bookSlot"));
            _window.ClearButton.OnPressed += _ => SendMessage(new BookPrinterClearContainerMessage());
            _window.UploadButton.OnPressed += _ => SendMessage(new BookPrinterUploadMessage());
            _window.CopyPasteButton.OnPressed += _ => SendMessage(new BookPrinterCopyPasteMessage());


            _window.OpenCentered();
            _window.OnClose += Close;

            _window.OnPrintBookButtonPressed += (args, button) => SendMessage(new BookPrinterPrintBookMessage(button.BookEntry));
            _window.OnPrintBookButtonMouseEntered += (args, button) =>
            {
                if (_lastState is not null)
                    _window.UpdateContainerInfo(_lastState);
            };
            _window.OnPrintBookButtonMouseExited += (args, button) =>
            {
                if (_lastState is not null)
                    _window.UpdateContainerInfo(_lastState);
            };
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (BookPrinterBoundUserInterfaceState)state;
            _lastState = castState;

            _window?.UpdateState(castState);
        }

        [Obsolete]
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}
