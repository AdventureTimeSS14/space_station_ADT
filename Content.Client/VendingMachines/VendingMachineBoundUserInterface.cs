using Content.Client.ADT.VendingMachines.UI;
using Content.Client.UserInterface.Controls;
using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using System.Linq;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private FancyVendingMachineMenu? _menu; // ADT-tweak - новое меню

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = new();  // ADT-tweak - новое меню
            var component = EntMan.GetComponent<VendingMachineComponent>(Owner); //ADT-Economy
            var system = EntMan.System<VendingMachineSystem>(); //ADT-Economy
            _cachedInventory = system.GetAllInventory(Owner, component); //ADT-Economy
            _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

            // ADT-tweak-start
            _menu.OnClose += Close;
            _menu.OnItemSelected += OnItemSelected;
            _menu.OnWithdraw += () => SendMessage(new VendingMachineWithdrawMessage());
            _menu.Populate(Owner, _cachedInventory, component.PriceMultiplier, component.Credits);
            // ADT-tweak-end

            _menu.OpenCentered();
        }

        public void Refresh()
        {
            var system = EntMan.System<VendingMachineSystem>();
            var component = EntMan.GetComponent<VendingMachineComponent>(Owner); //ADT-Economy
            _cachedInventory = system.GetAllInventory(Owner);

            _menu?.Populate(Owner, _cachedInventory, component.PriceMultiplier, component.Credits); //ADT-Economy-Tweak
        }


        // START-ADT-TWEAK
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var system = EntMan.System<VendingMachineSystem>();

            if (state is not VendingMachineInterfaceState newState)
                return;

            _cachedInventory = system.GetAllInventory(Owner);

            _menu?.Populate(Owner, _cachedInventory, newState.PriceMultiplier, newState.Credits); //ADT-Economy-Tweak
        }

        private void OnItemSelected(VendingMachineInventoryEntry entry)
        {
            SendPredictedMessage(new VendingMachineEjectCountMessage(entry, 1));
        }

        // END-ADT-TWEAK

        private void OnItemSelected(GUIBoundKeyEventArgs args, ListData data)
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            if (data is not VendorItemsListData { ItemIndex: var itemIndex })
                return;

            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(itemIndex);

            if (selectedItem == null)
                return;

            SendPredictedMessage(new VendingMachineEjectMessage(selectedItem.Type, selectedItem.ID));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            if (_menu == null)
                return;

            _menu.OnItemSelected -= OnItemSelected;    // ADT vending eject count
            _menu.OnClose -= Close;
            _menu.Dispose();
        }
    }
}
