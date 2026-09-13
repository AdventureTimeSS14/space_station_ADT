using Content.Client.ADT.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface;

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

        //ADT-Economy-Tweak start
        public void Refresh()
        {
            if (!IsOpened || _menu == null)
                return;

            if (State is not VendingMachineInterfaceState state)
                return;

            var system = EntMan.System<VendingMachineSystem>();
            _cachedInventory = system.GetAllInventory(Owner);

            _menu.Populate(Owner, _cachedInventory, state.PriceMultiplier, state.Credits);
        }
        //ADT-Economy-Tweak end

        public void UpdateAmounts()
        {
            //ADT-Economy-Tweak start
            if (!IsOpened || _menu == null)
                return;
            //ADT-Economy-Tweak end

            var enabled = EntMan.TryGetComponent(Owner, out VendingMachineComponent? bendy) && !bendy.Ejecting;

            var system = EntMan.System<VendingMachineSystem>();
            _cachedInventory = system.GetAllInventory(Owner);
            _menu.UpdateAmounts(_cachedInventory, enabled); //ADT-Economy-Tweak
        }

        // START-ADT-TWEAK
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not VendingMachineInterfaceState newState)
                return;

            _cachedInventory = newState.Inventory; //ADT-Economy-Tweak
            _menu?.Populate(Owner, _cachedInventory, newState.PriceMultiplier, newState.Credits); //ADT-Economy-Tweak
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);

            if (message is VendingMachineUserInfoMessage info)
                _menu?.SetUserInfo(info.Balance, info.IgnoreBalance); // ADT-Tweak
        }

        private void OnItemSelected(VendingMachineInventoryEntry entry, int count, Color? paintColor) // ADT-tweak
        {
            SendPredictedMessage(new VendingMachineEjectCountMessage(entry, count, paintColor)); // ADT-tweak
        }

        // END-ADT-TWEAK

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

