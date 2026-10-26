using Content.Client.ADT.Xenobiology.UI;
using Content.Shared.ADT.Xenobiology;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Xenobiology;

public sealed class SlimeScannerBoundUserInterface : BoundUserInterface
{
    private SlimeScannerWindow? _window;
    private SlimeScannerScannedMessage? _pendingMessage;

    public SlimeScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<SlimeScannerWindow>();
        _window.SetWindowTitle(EntMan.GetComponent<MetaDataComponent>(Owner).EntityName);
        
        if (_pendingMessage != null)
        {
            _window.Populate(_pendingMessage);
            _pendingMessage = null;
        }
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is not SlimeScannerScannedMessage msg)
            return;

        if (_window == null)
        {
            _pendingMessage = msg;
            return;
        }

        _window.Populate(msg);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _window?.Close();
        _window = null;
        _pendingMessage = null;
    }
}