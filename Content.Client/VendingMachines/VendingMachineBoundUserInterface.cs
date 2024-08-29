using Content.Client.VendingMachines.UI;
using Content.Shared.VendingMachines;
using Robust.Client.UserInterface.Controls;
using System.Linq;
using Robust.Client.UserInterface;

namespace Content.Client.VendingMachines
{
    public sealed class VendingMachineBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private VendingMachineMenu? _menu;

        [ViewVariables]
        private List<VendingMachineInventoryEntry> _cachedInventory = new();

        [ViewVariables]
        private List<int> _cachedFilteredIndex = new();

        public VendingMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            var vendingMachineSys = EntMan.System<VendingMachineSystem>();

            var component = EntMan.GetComponent<VendingMachineComponent>(Owner); //ADT-Economy
            _cachedInventory = vendingMachineSys.GetAllInventory(Owner, component); //ADT-Economy

            _menu = this.CreateWindow<VendingMachineMenu>();
            _menu.OpenCenteredLeft();
            _menu.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

            _menu.OnClose += Close; //ADT-Economy
            _menu.OnItemSelected += OnItemSelected;
            _menu.OnSearchChanged += OnSearchChanged;
            _menu.OnWithdraw += SendMessage; //ADT-Economy

            _menu.Populate(Owner, _cachedInventory, out _cachedFilteredIndex, component.PriceMultiplier, component.Credits); //ADT-Economy
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (state is not VendingMachineInterfaceState newState)
                return;

            _cachedInventory = newState.Inventory;

            _menu?.Populate(Owner, _cachedInventory, out _cachedFilteredIndex, newState.PriceMultiplier, newState.Credits, _menu.SearchBar.Text); //ADT-Economy
        }

        private void OnItemSelected(ItemList.ItemListSelectedEventArgs args)
        {
            if (_cachedInventory.Count == 0)
                return;

            var selectedItem = _cachedInventory.ElementAtOrDefault(_cachedFilteredIndex.ElementAtOrDefault(args.ItemIndex));

            if (selectedItem == null)
                return;

            SendMessage(new VendingMachineEjectMessage(selectedItem.Type, selectedItem.ID));
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

        private void OnSearchChanged(string? filter)
        {
            //ADT-Economy-Start
            var component = EntMan.GetComponent<VendingMachineComponent>(Owner);
            _menu?.Populate(Owner, _cachedInventory, out _cachedFilteredIndex, component.PriceMultiplier, component.Credits, filter);
            //ADT-Economy-End
        }
    }
}
