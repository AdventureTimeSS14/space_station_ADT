using Content.Client.ADT.UI;
using Content.Shared.ADT.Rituals;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Rituals.UI;

public sealed class ADTRitualSummonBoundUserInterface : BoundUserInterface
{
    private ADTEntityPickerWindow? _window;

    public ADTRitualSummonBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ADTEntityPickerWindow>();
        _window.SetText(
            Loc.GetString("adt-ritual-summon-window-title"),
            Loc.GetString("adt-ritual-summon-window-hint"));
        _window.OnEntrySelected += OnSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null || state is not ADTRitualSummonBuiState summonState)
            return;

        _window.SetEntries(summonState.Candidates);
    }

    private void OnSelected(NetEntity target)
    {
        SendMessage(new ADTRitualSummonSelectMessage(target));
        Close();
    }
}
