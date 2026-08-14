using Content.Shared.ADT.AshWalker;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.AshWalker.UI;

public sealed class ADTNecropolisCompassBoundUserInterface : BoundUserInterface
{
    private ADTNecropolisCompassWindow? _window;

    public ADTNecropolisCompassBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ADTNecropolisCompassWindow>();
        _window.OnPointSelected += OnPointSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not ADTNecropolisCompassBuiState compassState)
            return;

        _window.SetPoints(compassState.Points);
    }

    private void OnPointSelected(NetEntity point)
    {
        SendMessage(new ADTNecropolisCompassSelectMessage(point));
        Close();
    }
}
