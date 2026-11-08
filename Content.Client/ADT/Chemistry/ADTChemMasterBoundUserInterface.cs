using Content.Shared.ADT.Chemistry;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Chemistry
{
    /// <summary>
    /// Initializes a <see cref="ADTChemMasterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class ADTChemMasterBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey) //ADT-Tweak
    {
        [ViewVariables]
        private ADTChemMasterWindow? _window;

        // ADT-Tweak: Cutted
        // public ADTChemMasterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        // {
        // }

        /// <summary>
        /// Called each time a chem master UI instance is opened. Generates the window and fills it with
        /// relevant info. Sets the actions for static buttons.
        /// </summary>
        protected override void Open()
        {
            base.Open();

            // Setup window layout/elements
            _window = this.CreateWindow<ADTChemMasterWindow>();
            _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

            // Setup static button actions.
            _window.InputEjectButton.OnPressed += _ => SendMessage(
                new ItemSlotButtonPressedEvent(ADTADTChemMaster.InputSlotName));
            // _window.OutputEjectButton.OnPressed += _ => SendMessage(
            //     new ItemSlotButtonPressedEvent(ADTADTChemMaster.OutputSlotName)); // ADT-Tweak: Cutted
            _window.BufferTransferButton.OnPressed += _ => SendMessage(
                new ADTChemMasterSetModeMessage(ADTChemMasterMode.Transfer));
            _window.BufferDiscardButton.OnPressed += _ => SendMessage(
            //ADT-Tweak-Start
                new ADTChemMasterSetModeMessage(ADTChemMasterMode.Discard));
            _window.CreatePillButton.OnPressed += _ => HandleCreatePillPressed();

            // ADT-Tweak: Cutted
            // _window.CreatePillButton.OnPressed += _ => SendMessage(
            //     new ADTChemMasterCreatePillsMessage(
            //         (uint) _window.PillDosage.Value, (uint) _window.PillNumber.Value, _window.LabelLine));
            // _window.CreateBottleButton.OnPressed += _ => SendMessage(
            //     new ADTChemMasterOutputToBottleMessage(
            //         (uint) _window.BottleDosage.Value, _window.LabelLine));
            // _window.BufferSortButton.OnPressed += _ => SendMessage(
            //         new ADTChemMasterSortingTypeCycleMessage());

            _window.CreateBottleButton.OnPressed += _ => HandleCreateBottlePressed(); //ADT-Tweak

            for (uint i = 0; i < _window.PillTypeButtons.Length; i++)
            {
                var pillType = i;
                _window.PillTypeButtons[i].OnPressed += _ => SendMessage(new ADTChemMasterSetPillTypeMessage(pillType));
            }
            // ADT-Tweak-Start
            // Transfer buttons
            _window.OnReagentButtonPressed += (_, button, amount, isOutput) => SendMessage(new ADTChemMasterReagentAmountButtonMessage(button.Id, amount, button.IsBuffer, isOutput));
            _window.OnSortMethodChanged += sortMethod => SendMessage(new ADTChemMasterSortMethodUpdated(sortMethod));
            _window.OnTransferAmountChanged += amount => SendMessage(new ADTChemMasterTransferringAmountUpdated(amount));
            _window.OnUpdateAmounts += amounts => SendMessage(new ADTChemMasterAmountsUpdated(amounts));
            _window.OnTransferAllPressed += (reagent, isBuffer, isOutput) => SendMessage(new ADTChemMasterReagentAmountButtonMessage(reagent, int.MaxValue, isBuffer, isOutput));
            _window.OnChooseReagentPressed += reagent => SendMessage(!string.IsNullOrEmpty(reagent.Prototype) ? new ADTChemMasterChooseReagentMessage(reagent) : new ADTChemMasterClearReagentSelectionMessage());
            _window.OnToggleBottleFillPressed += slot => SendMessage(new ADTChemMasterToggleBottleFillMessage(slot));
            // Per-slot eject: mimic dispenser card eject button behavior, addressing item slot by its ID (bottleSlot{index})
            _window.OnBottleSlotEjectPressed += slot => SendMessage(new ItemSlotButtonPressedEvent($"bottleSlot{slot}"));
            _window.OnRowEjectPressed += row => SendMessage(new ADTChemMasterRowEjectMessage(row));
            //  Pill container event handlers
            _window.OnPillContainerSlotSelected += slot => SendMessage(new ADTChemMasterSelectPillContainerSlotMessage(slot));
            _window.OnTogglePillContainerFillPressed += slot => SendMessage(new ADTChemMasterTogglePillContainerFillMessage(slot));
            _window.OnPillCanisterSelected += canisterIndex => SendMessage(new ADTChemMasterSelectPillCanisterForCreationMessage(canisterIndex));
            _window.OnPillCanisterEjected += canisterIndex => SendMessage(new ItemSlotButtonPressedEvent($"pillContainerSlot{canisterIndex}"));
            // Reagent amount selection handlers
            _window.OnSelectReagentAmount += (reagent, amount) => SendMessage(new ADTChemMasterSelectReagentAmountMessage(reagent, amount));
            _window.OnRemoveReagentAmount += (reagent, amount) => SendMessage(new ADTChemMasterRemoveReagentAmountMessage(reagent, amount));
            _window.OnClearReagentAmount += reagent => SendMessage(new ADTChemMasterClearReagentAmountMessage(reagent));
            // Transfer reagent from bottle to buffer
            _window.OnTransferReagentFromBottle += (reagent, amount) => SendMessage(new ADTChemMasterReagentAmountButtonMessage(reagent, amount, false, false));
            // ADT-Tweak-End
        }

        /// <summary>
        /// Update the ui each time new state data is sent from the server.
        /// </summary>
        /// <param name="state">
        /// Data of the <see cref="SharedReagentDispenserComponent"/> that this ui represents.
        /// Sent from the server.
        /// </param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ADTChemMasterBoundUserInterfaceState) state;
            _window?.UpdateState(castState); // Update window state
        }
        // ADT-Tweak-Start: Creation Handlers
        private void HandleCreatePillPressed()
        {
            if (_window == null) return;
            var pillLabel = _window.GeneratePillLabel();
            SendMessage(new ADTChemMasterCreatePillsMessage(
                (uint)_window.PillDosage.Value,
                (uint)_window.PillNumber.Value,
                pillLabel));
        }

        private void HandleCreateBottlePressed()
        {
            if (_window == null) return;
            var bottleLabel = _window.GenerateBottleLabel();
            SendMessage(new ADTChemMasterOutputToBottleMessage(
                (uint)_window.BottleDosage.Value,
                (uint)_window.BottleNumber.Value,
                bottleLabel));
        }
        // ADT-Tweak-End: Creation Handlers
    }
}
