using Content.Shared.ADT.Rituals;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Rituals.UI;

public sealed class ADTRitualBoundUserInterface : BoundUserInterface
{
    private ADTRitualMenuWindow? _window;

    public ADTRitualBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ADTRitualMenuWindow>();
        _window.OnStartPressed += ritual => SendMessage(new ADTRitualStartMessage(ritual));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not ADTRitualBuiState ritualState)
            return;

        _window.SetState(ritualState);
    }
}
