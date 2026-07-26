using Content.Shared.ADT.Surgery.Events;
using JetBrains.Annotations;

namespace Content.Client.ADT.Surgery.UI;

[UsedImplicitly]
public sealed class SurgeryConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SurgeryConsoleWindow? _window;

    public SurgeryConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new SurgeryConsoleWindow
        {
            Title = Loc.GetString("surgery-console-window-title"),
        };
        _window.OnClose += Close;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not SurgeryConsoleState cast)
            return;

        _window.Populate(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
            _window.OnClose -= Close;

        _window?.Dispose();
    }
}
