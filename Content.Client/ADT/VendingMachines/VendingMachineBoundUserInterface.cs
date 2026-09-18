using Content.Client.ADT.VendingMachines.UI;
using Content.Shared.ADT.VendingMachines;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.VendingMachines;

public sealed class VendingMachineBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private FancyVendingMachineMenu? _menu;

    [ViewVariables]
    private List<VendingMachineInventoryEntry> _cachedInventory = new();

    public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = new();
        var component = EntMan.GetComponent<VendingMachineComponent>(Owner);
        var system = EntMan.System<VendingMachineSystem>();
        _cachedInventory = system.GetAllInventory(Owner, component);
        _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _menu.OnClose += Close;
        _menu.OnItemSelected += OnItemSelected;
        _menu.OnWithdraw += () => SendMessage(new VendingMachineWithdrawMessage());
        _menu.Populate(Owner, _cachedInventory, component.PriceMultiplier, component.Credits,
            VendingMachineHelpers.GetReturnedItemEntities(EntMan, Owner));

        _menu.OpenCentered();
    }

    public void Refresh()
    {
        if (!IsOpened || _menu == null)
            return;

        if (State is not VendingMachineInterfaceState state)
            return;

        var system = EntMan.System<VendingMachineSystem>();
        _cachedInventory = system.GetAllInventory(Owner);

        _menu.Populate(Owner, _cachedInventory, state.PriceMultiplier, state.Credits, state.ReturnedEntities);
    }

    public void UpdateAmounts()
    {
        if (!IsOpened || _menu == null)
            return;

        var enabled = EntMan.TryGetComponent(Owner, out VendingMachineComponent? bendy) && !bendy.Ejecting;

        var system = EntMan.System<VendingMachineSystem>();
        _cachedInventory = system.GetAllInventory(Owner);
        _menu.UpdateAmounts(_cachedInventory, enabled);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not VendingMachineInterfaceState newState)
            return;

        _cachedInventory = newState.Inventory;
        _menu?.Populate(Owner, _cachedInventory, newState.PriceMultiplier, newState.Credits, newState.ReturnedEntities);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is VendingMachineUserInfoMessage info)
            _menu?.SetUserInfo(info.Balance, info.IgnoreBalance);
    }

    private void OnItemSelected(VendingMachineInventoryEntry entry, int count, Color? paintColor)
    {
        SendPredictedMessage(new VendingMachineEjectCountMessage(entry, count, paintColor));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_menu == null)
            return;

        _menu.OnItemSelected -= OnItemSelected;
        _menu.OnClose -= Close;
        _menu.Dispose();
    }
}