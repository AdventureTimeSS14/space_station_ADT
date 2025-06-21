using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.CommandConsole
{
    [UsedImplicitly]
    public sealed class CommandConsoleBoundUserInterface : BoundUserInterface
    {
        private CommandConsoleWindow? _window;
        private readonly EntityUid _owner;

        public CommandConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            IoCManager.InjectDependencies(this);
            _owner = owner;
        }

        protected override void Open()
        {
            base.Open();

            _window = new CommandConsoleWindow(this, _owner);
            _window.OpenCentered();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Close();
                _window = null;
            }
        }
    }
}
