using Content.Client.ADT.UI;
using Content.Shared.ADT.AshWalker;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.AshWalker.UI;

public sealed class ADTNecropolisCompassBoundUserInterface : BoundUserInterface
{
    private ADTEntityPickerWindow? _window;

    public ADTNecropolisCompassBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ADTEntityPickerWindow>();
        _window.SetText(
            Loc.GetString("adt-necropolis-compass-window-title"),
            Loc.GetString("adt-necropolis-compass-window-hint"));
        _window.OnEntrySelected += OnPointSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not ADTNecropolisCompassBuiState compassState)
            return;

        _window.SetEntries(compassState.Points);
    }

    private void OnPointSelected(NetEntity point)
    {
        SendMessage(new ADTNecropolisCompassSelectMessage(point));
        Close();
    }
}
