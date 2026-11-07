using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.Casino;

namespace Content.Client.ADT.Casino;

public sealed class SlotMachineCasinoBoundUserInterface : BoundUserInterface
{
    private SlotMachineCasinoMenu? _menu;

    public SlotMachineCasinoBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new SlotMachineCasinoMenu(this);
        _menu.OnClose += Close;
        _menu.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is SlotMachineCasinoBoundUserInterfaceState casinoState)
            _menu?.UpdateState(casinoState);
    }

    public void SendAction(SlotMachineCasinoAction action, int amount = 0, string choice = "")
    {
        SendMessage(new SlotMachineCasinoActionMessage(action, amount, choice));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Dispose();
        _menu = null;
    }
}
