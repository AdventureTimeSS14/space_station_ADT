using Content.Shared.ADT.Holomap;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Holomap.UI;

[UsedImplicitly]
public sealed class HolomapBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private HolomapWindow? _window;

    public HolomapBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = new HolomapWindow();
        _window.OnClose += Close;
        _window.OnModeSelected += OnModeSelected;
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
    }

    private void OnModeSelected(HolomapMode mode)
    {
        SendMessage(new HolomapModeSelectedMessage(mode));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _window?.Dispose();
        }
    }
}
