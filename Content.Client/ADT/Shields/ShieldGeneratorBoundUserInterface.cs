using Content.Client.ADT.Shields.UI;
using Content.Shared.ADT.Shields;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Shields;

[UsedImplicitly]
public sealed class ShieldGeneratorBoundUserInterface : BoundUserInterface
{
    private ShieldGeneratorWindow? _window;

    public ShieldGeneratorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ShieldGeneratorWindow>();
        _window.OnSendMessage += SendMessage;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ShieldGeneratorBuiState castState)
            _window?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_window == null)
            return;

        _window.OnSendMessage -= SendMessage;
        _window.Dispose();
        _window = null;
    }
}