using Robust.Client.UserInterface;
using Content.Shared.Supermatter.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Content.Client.Supermatter;

namespace Content.Client.Supermatter;

public sealed class SupermatterConsoleBoundUserInterface : BoundUserInterface
{
	[ViewVariables]
    private SupermatterControlWindow? _window;

    public SupermatterConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<SupermatterControlWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (_window == null || state is not SupermatterConsoleUpdateState cast) return;
        _window.UpdatePercents(cast.Procents);
        _window.UpdateGases(cast.Gases);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _window?.Dispose();
    }
}