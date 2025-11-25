using Content.Server.Chemistry.Components;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
// ADT-Tweak-Start
using Content.Shared.Whitelist;
using Content.Shared.Tag;
using Content.Shared.Labels.EntitySystems;
// ADT-Tweak-End

namespace Content.Server.Chemistry.EntitySystems
{

    /// <summary>
    /// Contains all the server-side logic for ChemMasters.
    /// <seealso cref="ChemMasterComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemMasterSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly StorageSystem _storageSystem = default!;
        [Dependency] private readonly LabelSystem _labelSystem = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!; //ADT-Tweak

        private static readonly EntProtoId PillPrototypeId = "Pill";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChemMasterComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            // ADT-Tweak Start: Cutted
            // SubscribeLocalEvent<ChemMasterComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            // SubscribeLocalEvent<ChemMasterComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            // ADT-Tweak End
            SubscribeLocalEvent<ChemMasterComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            // ADT-Tweak Start: Bottle/Pill Containers now updates individually and separately from UI window.
            SubscribeLocalEvent<ChemMasterComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
            SubscribeLocalEvent<ChemMasterComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
            // ADT-Tweak End

            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetModeMessage>(OnSetModeMessage);
            // SubscribeLocalEvent<ChemMasterComponent, ChemMasterSortingTypeCycleMessage>(OnCycleSortingTypeMessage); // ADT-Tweak: Cutted
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetPillTypeMessage>(OnSetPillTypeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterReagentAmountButtonMessage>(OnReagentButtonMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterCreatePillsMessage>(OnCreatePillsMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterOutputToBottleMessage>(OnOutputToBottleMessage);

            // ADT-Tweak Start: Subscriptions for all buttons
            // Handle client sort dropdown/button interactions.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSortMethodUpdated>(OnSortMethodUpdated);
            // Update the currently selected transfer amount coming from the UI.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterTransferringAmountUpdated>(OnTransferringAmountUpdated);
            // Sync the list of custom amount buttons the player configured.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterAmountsUpdated>(OnAmountsUpdated);
            // Track which output bottle slot the player highlighted for context actions.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSelectBottleSlotMessage>(OnSelectBottleSlotMessage);
            // Handle choosing a reagent to operate on.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterChooseReagentMessage>(OnChooseReagentMessage);
            // Clear all currently selected reagents when the user requests it.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterClearReagentSelectionMessage>(OnClearReagentSelectionMessage);
            // Toggle whether a given bottle slot should receive automatic fills.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterToggleBottleFillMessage>(OnToggleBottleFillMessage);
            // Process eject-all actions for an entire bottle row at once.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterRowEjectMessage>(OnRowEjectMessage);
            // Remember which pill container slot is being targeted for edits.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSelectPillContainerSlotMessage>(OnSelectPillContainerSlotMessage);
            // Automatically fills pill container slots.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterTogglePillContainerFillMessage>(OnTogglePillContainerFillMessage);
            // Eject a single pill slot contents back into the world.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterPillContainerSlotEjectMessage>(OnPillContainerSlotEjectMessage);
            // Eject an entire pill container row in one action.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterPillContainerRowEjectMessage>(OnPillContainerRowEjectMessage);
            // Decide which pill canister incoming pills should spawn into.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSelectPillCanisterForCreationMessage>(OnSelectPillCanisterForCreationMessage);
            // Track per-reagent amount selections for batch editing.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSelectReagentAmountMessage>(OnSelectReagentAmountMessage);
            // Decrease/remove a stored reagent amount selection.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterRemoveReagentAmountMessage>(OnRemoveReagentAmountMessage);
            // Reset all tracked reagent amount selections in one go.
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterClearReagentAmountMessage>(OnClearReagentAmountMessage);
            // Respond to input/output slot button presses (insert/eject).
            SubscribeLocalEvent<ChemMasterComponent, ItemSlotButtonPressedEvent>(OnItemSlotButtonPressed);
            // Initialize when spawned in the world.
            SubscribeLocalEvent<ChemMasterComponent, MapInitEvent>(OnMapInit);
            // ADT-Tweak End
        }

        private void OnAmountsUpdated(Entity<ChemMasterComponent> ent, ref ChemMasterAmountsUpdated args)
        {
            ent.Comp.Amounts = args.Amounts;    // ADT-Tweak
            UpdateUiState(ent);
        }

        // ADT-Tweak Start
        private void SubscribeUpdateUiState<T>(Entity<ChemMasterComponent> ent, ref T ev) =>
            UpdateUiState(ent);
        // ADT-Tweak End

        private void UpdateUiState(Entity<ChemMasterComponent> ent, bool updateLabel = false)
        {
            var (owner, chemMaster) = ent;

            if (!_solutionContainerSystem.TryGetSolution(owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                return;

            // ADT-Tweak Start: Clean up selected reagent amounts for reagents that are no longer in the buffer
            // Also clamp selected amounts to available amounts
            var reagentsToRemove = new List<ReagentId>();
            var reagentsToUpdate = new Dictionary<ReagentId, float>();

            foreach (var (reagentId, selectedAmount) in chemMaster.SelectedReagentAmounts)
            {
                var availableAmount = bufferSolution.GetReagentQuantity(reagentId);

                if (availableAmount <= 0)
                {
                    // Reagent is completely gone from buffer - remove from selection
                    reagentsToRemove.Add(reagentId);
                }
                else if (selectedAmount > (float)availableAmount)
                {
                    // Selected amount exceeds available amount - clamp to available
                    reagentsToUpdate[reagentId] = (float)availableAmount;
                }
            }

            foreach (var reagentId in reagentsToRemove)
            {
                chemMaster.SelectedReagentAmounts.Remove(reagentId);
            }

            foreach (var (reagentId, newAmount) in reagentsToUpdate)
            {
                chemMaster.SelectedReagentAmounts[reagentId] = newAmount;
            }

            // Reset bottle selection if the selected bottle no longer exists or is no longer empty
            if (chemMaster.SelectedBottleForFill >= 0 && chemMaster.SelectedBottleForFill < 20)
            {
                // Will be populated during UI update below
                var slotId = "bottleSlot" + chemMaster.SelectedBottleForFill;
                if (_itemSlotsSystem.TryGetSlot(owner, slotId, out var bottleSlot) && bottleSlot.Item.HasValue)
                {
                    var bottle = bottleSlot.Item.Value;
                }
                else
                {
                    // Bottle no longer exists in the slot
                    chemMaster.SelectedBottleForFill = -1;
                }
            }

            // Initialize stored pill containers list with correct size if needed (3 containers)
            if (chemMaster.StoredPillContainers.Count != 3)
            {
                chemMaster.StoredPillContainers.Clear();
                for (int i = 0; i < 3; i++)
                    chemMaster.StoredPillContainers.Add(null);
            }

            // Initialize stored bottles list with correct size if needed
            if (chemMaster.StoredBottles.Count != 20)
            {
                chemMaster.StoredBottles.Clear();
                for (int i = 0; i < 20; i++)
                    chemMaster.StoredBottles.Add(null);
            }
            //ADT-Tweak End

            var container = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.InputSlotName); //ADT-Tweak
            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.Volume;

            // ADT-Tweak Start: Pill container storage
            var storedPillContainersInfo = new List<ContainerInfo?>();
            var pillContainers = new List<List<bool>>();
            var pillTypes = new List<List<uint>>();
            chemMaster.StoredPillContainers.Clear();

            for (int i = 0; i < 3; i++)
            {
                var slotId = "pillContainerSlot" + i;

                if (_itemSlotsSystem.TryGetSlot(owner, slotId, out var slot) && slot.Item.HasValue)
                {
                    var pillContainer = slot.Item.Value;

                    // For pill containers, we need to check if they have storage component and count pills
                    if (TryComp(pillContainer, out StorageComponent? storage))
                    {
                        var pillCount = storage.Container.ContainedEntities.Count;
                        var maxPills = 10; // Standard pill canister capacity

                        // Create bool list for occupied slots
                        var slotStates = new List<bool>();
                        var slotPillTypes = new List<uint>();

                        for (int j = 0; j < maxPills; j++)
                        {
                            if (j < pillCount)
                            {
                                slotStates.Add(true);

                                // Get the pill entity and its type
                                var pillEntity = storage.Container.ContainedEntities.ElementAt(j);
                                if (TryComp<PillComponent>(pillEntity, out var pillComponent))
                                {
                                    slotPillTypes.Add(pillComponent.PillType);
                                }
                                else
                                {
                                    slotPillTypes.Add(0); // Default pill type if no component found
                                }
                            }
                            else
                            {
                                slotStates.Add(false);
                                slotPillTypes.Add(0); // No pill in this slot
                            }
                        }

                        pillContainers.Add(slotStates);
                        pillTypes.Add(slotPillTypes);
                        storedPillContainersInfo.Add(new ContainerInfo(Name(pillContainer), pillCount, maxPills));
                    }
                    else
                    {
                        // Create empty container info for pill containers without storage
                        pillContainers.Add(Enumerable.Repeat(false, 10).ToList());
                        pillTypes.Add(Enumerable.Repeat((uint)0, 10).ToList());
                        storedPillContainersInfo.Add(new ContainerInfo(Name(pillContainer), 0, 10));
                    }

                    chemMaster.StoredPillContainers.Add(pillContainer);
                }
                else
                {
                    pillContainers.Add(Enumerable.Repeat(false, 10).ToList());
                    pillTypes.Add(Enumerable.Repeat((uint)0, 10).ToList());
                    storedPillContainersInfo.Add(null);
                    chemMaster.StoredPillContainers.Add(null);
                }
            }

            // ADT-Tweak Start: Bottle container storage
            var storedBottlesInfo = new List<ContainerInfo?>();
            chemMaster.StoredBottles.Clear();

            for (int i = 0; i < 20; i++)
            {
                var slotId = "bottleSlot" + i;

                if (_itemSlotsSystem.TryGetSlot(owner, slotId, out var slot) && slot.Item.HasValue)
                {
                    var bottle = slot.Item.Value;

                    // Try to get the bottle solution, but don't fail if it doesn't exist
                    if (_solutionContainerSystem.TryGetSolution(bottle, SharedChemMaster.BottleSolutionName, out _, out var sol))
                    {
                        storedBottlesInfo.Add(BuildContainerInfo(Name(bottle), sol));
                    }
                    else
                    {
                        // Create empty container info for bottles without solution
                        storedBottlesInfo.Add(new ContainerInfo(Name(bottle), 0, FixedPoint2.New(50))); // Assume 50u capacity
                    }

                    chemMaster.StoredBottles.Add(bottle); // Add to list in correct order
                }
                else
                {
                    storedBottlesInfo.Add(null);
                    chemMaster.StoredBottles.Add(null);
                }
            }
            // ADT-Tweak End

            // ADT-Tweak Start: Updated Constructor
            var state = new ChemMasterBoundUserInterfaceState(
                chemMaster.Mode,
                BuildInputContainerInfo(container),
                bufferReagents,
                bufferCurrentVolume,
                chemMaster.PillType,
                chemMaster.PillDosageLimit,
                chemMaster.BottleDosageLimit,
                updateLabel,
                chemMaster.SortMethod,
                chemMaster.TransferringAmount,
                chemMaster.Amounts,
                storedPillContainersInfo,
                pillContainers,
                pillTypes,
                chemMaster.SelectedPillContainerSlot,
                chemMaster.SelectedPillContainerForFill,
                chemMaster.SelectedPillCanisterForCreation,
                chemMaster.SelectedReagent,
                storedBottlesInfo,
                chemMaster.SelectedBottleSlot,
                chemMaster.SelectedBottleForFill,
                chemMaster.SelectedReagents,
                chemMaster.SelectedReagentAmounts);
            //ADT-Tweak End

            _userInterfaceSystem.SetUiState(owner, ChemMasterUiKey.Key, state);
        }

        private void OnSetModeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSetModeMessage message)
        {
            // Ensure the mode is valid, either Transfer or Discard.
            if (!Enum.IsDefined(typeof(ChemMasterMode), message.ChemMasterMode))
                return;

            chemMaster.Comp.Mode = message.ChemMasterMode;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        // ADT-Tweak Start: Cutted
        // private void OnCycleSortingTypeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSortingTypeCycleMessage message)
        // {
        //     chemMaster.Comp.SortingType++;
        //     if (chemMaster.Comp.SortingType > ChemMasterSortingType.Latest)
        //         chemMaster.Comp.SortingType = ChemMasterSortingType.None;
        //     UpdateUiState(chemMaster);
        //     ClickSound(chemMaster);
        // }
        // ADT-Tweak End

        private void OnSetPillTypeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSetPillTypeMessage message)
        {
            // Ensure valid pill type. There are 20 pills selectable, 0-19.
            if (message.PillType > SharedChemMaster.PillTypes - 1)
                return;

            chemMaster.Comp.PillType = message.PillType;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnReagentButtonMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterReagentAmountButtonMessage message)
        {
            // ADT-Tweak Start: Cutted
            // // Ensure the amount corresponds to one of the reagent amount buttons.
            // if (!Enum.IsDefined(typeof(ChemMasterReagentAmount), message.Amount))
            //     return;
            // ADT-Tweak End

            switch (chemMaster.Comp.Mode)
            {
                case ChemMasterMode.Transfer:
                    TransferReagents(chemMaster, message.ReagentId, message.Amount, message.FromBuffer, message.IsOutput);
                    break;
                case ChemMasterMode.Discard:
                    DiscardReagents(chemMaster, message.ReagentId, message.Amount, message.FromBuffer, message.IsOutput);
                    break;
                default:
                    // Invalid mode.
                    return;
            }

            ClickSound(chemMaster);
        }

        // ADT-Tweak Start: Bottle buttons reagent transfer
        private void OnContainerInserted(Entity<ChemMasterComponent> chemMaster, ref EntInsertedIntoContainerMessage args)
        {
            // Check if this insertion is into one of our pill container slots
            if (args.Container?.ID?.StartsWith("pillContainerSlot") == true)
            {
                UpdateUiState(chemMaster);
                return;
            }

            // Check if this insertion is into one of our pill containers (storage)
            if (IsPillContainerStorage(chemMaster, args.Container?.Owner))
            {
                UpdateUiState(chemMaster);
                return;
            }

            // Always handle UI refresh on inserts.
            if (args.Container?.ID == SharedChemMaster.OutputSlotName)
            {
                var entity = args.Entity;

                // If a bottle was inserted into the input slot, relocate it to the first free bottle slot
                // in row-major order (left-to-right, top-to-bottom).
                if (_solutionContainerSystem.TryGetSolution(entity, SharedChemMaster.BottleSolutionName, out _, out _))
                {
                    // Ensure the bottle is not still inside the input slot before relocating it.
                    // Capture the ejected entity and use it for insertion to avoid timing issues.
                    var owner = chemMaster;



                    _itemSlotsSystem.TryEject(owner, SharedChemMaster.OutputSlotName, null, out var ejected, excludeUserAudio: true);
                    var moving = ejected ?? entity;

                    for (int row = 0; row < 4; row++)
                    {
                        for (int col = 0; col < 5; col++)
                        {
                            var i = row * 5 + col;
                            var slotId = "bottleSlot" + i;

                            // Use the machine UID explicitly to query slots.
                            if (_itemSlotsSystem.TryGetSlot(owner, slotId, out var slot) && !slot.HasItem)
                            {
                                if (_itemSlotsSystem.TryInsert(owner, slotId, moving, null))
                                {
                                    // Bottle moved successfully: pack into row-major order, update UI, and play feedback.
                                    UpdateUiState(chemMaster);
                                    ClickSound(chemMaster);
                                    return;
                                }
                            }
                        }
                    }

                }

                // Non-bottle container (e.g. beaker) or relocation failed: refresh to show the correct input container state.
                UpdateUiState(chemMaster);
                return;
            }

            // Insertions into other containers (e.g., bottleSlots) -> pack slots then refresh UI and feedback.
            UpdateUiState(chemMaster);
        }


        private void OnContainerRemoved(Entity<ChemMasterComponent> chemMaster, ref EntRemovedFromContainerMessage args)
        {
            // Check if this removal is from one of our pill containers
            if (IsPillContainerStorage(chemMaster, args.Container?.Owner))
            {
                UpdateUiState(chemMaster);
                return;
            }

            // Skip transient removal updates during controlled relocation from input slot.
            var owner = chemMaster;
            // Determine which slot triggered the removal by container ID.
            // Only repack if the removal came from one of the bottle grid slots (bottleSlot0..19).
            var containerId = args.Container?.ID;
            var removedFromBottleSlot = containerId != null && containerId.StartsWith("bottleSlot");

            if (removedFromBottleSlot && containerId != null)
            {
                if (_itemSlotsSystem.TryGetSlot(owner, containerId, out var slot) && slot.Item.HasValue)
                    _itemSlotsSystem.TryEject(owner, slot, null, out _, excludeUserAudio: true);
            }

            // Always refresh UI (covers beaker removal and any other container content changes).
            UpdateUiState(chemMaster);
        }

        // Checks if the given storage entity is one of our pill containers.
        private bool IsPillContainerStorage(Entity<ChemMasterComponent> chemMaster, EntityUid? storageEntity)
        {
            if (storageEntity == null)
                return false;

            // Check if the storage entity is in any of the 3 pill container slots
            for (int i = 0; i < 3; i++)
            {
                if (chemMaster.Comp.StoredPillContainers.Count > i &&
                    chemMaster.Comp.StoredPillContainers[i] == storageEntity)
                {
                    return true;
                }
            }
            return false;
        }

        // Finds available empty bottles for filling across all bottle slots
        private List<EntityUid> FindAvailableBottlesForFilling(Entity<ChemMasterComponent> chemMaster, int requestedBottles, uint dosage)
        {
            var targets = new List<EntityUid>(capacity: requestedBottles);

            // Find empty bottles across all slots, regardless of selection
            for (int i = 0; i < chemMaster.Comp.StoredBottles.Count && targets.Count < requestedBottles; i++)
            {
                var ent = chemMaster.Comp.StoredBottles[i];
                if (!ent.HasValue)
                    continue;

                if (!_solutionContainerSystem.TryGetSolution(ent.Value, SharedChemMaster.BottleSolutionName, out var soln, out var solution))
                    continue;

                // Only select empty bottles with enough free volume for the dosage
                if (solution.Volume == 0 && solution.AvailableVolume >= dosage)
                    targets.Add(ent.Value);
            }

            return targets;
        }

        /// Finds available empty slots for pill creation, either in the selected canister or all canisters. Returns a list of (container, startSlot) pairs for filling
        private (int totalAvailableSlots, List<(EntityUid container, int startSlot)> availableSlots) FindAvailablePillSlotsForCreation(Entity<ChemMasterComponent> chemMaster, int requestedPills)
        {
            var availableSlots = new List<(EntityUid, int)>();
            var totalAvailableSlots = 0;

            // If a specific canister is selected, only use that one
            if (chemMaster.Comp.SelectedPillCanisterForCreation >= 0 && chemMaster.Comp.SelectedPillCanisterForCreation < 3)
            {
                var selectedContainerIndex = chemMaster.Comp.SelectedPillCanisterForCreation;
                var slotId = "pillContainerSlot" + selectedContainerIndex;

                if (_itemSlotsSystem.TryGetSlot(chemMaster, slotId, out var slot) && slot.Item.HasValue)
                {
                    var pillContainer = slot.Item.Value;

                    if (TryComp(pillContainer, out StorageComponent? storage))
                    {
                        var currentPillCount = storage.Container.ContainedEntities.Count;
                        var maxPills = 10; // Standard pill canister capacity
                        var emptySlotsInContainer = maxPills - currentPillCount;

                        if (emptySlotsInContainer > 0)
                        {
                            totalAvailableSlots = Math.Min(emptySlotsInContainer, requestedPills);
                            availableSlots.Add((pillContainer, currentPillCount)); // Start filling from where pills end
                        }
                    }
                }
            }
            else
            {
                // No canister selected - find slots across all canisters
                for (int containerIndex = 0; containerIndex < 3 && totalAvailableSlots < requestedPills; containerIndex++)
                {
                    var slotId = "pillContainerSlot" + containerIndex;

                    if (_itemSlotsSystem.TryGetSlot(chemMaster, slotId, out var slot) && slot.Item.HasValue)
                    {
                        var pillContainer = slot.Item.Value;

                        if (TryComp(pillContainer, out StorageComponent? storage))
                        {
                            var currentPillCount = storage.Container.ContainedEntities.Count;
                            var maxPills = 10; // Standard pill canister capacity
                            var emptySlotsInContainer = maxPills - currentPillCount;
                            var slotsNeeded = requestedPills - totalAvailableSlots;
                            var slotsToUse = Math.Min(emptySlotsInContainer, slotsNeeded);

                            if (slotsToUse > 0)
                            {
                                availableSlots.Add((pillContainer, currentPillCount)); // Start filling from where pills end
                                totalAvailableSlots += slotsToUse;
                            }
                        }
                    }
                }
            }

            return (totalAvailableSlots, availableSlots);
        }


        // ADT-Tweak-Start: Transferring reagents
        private void TransferReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer, bool isOutput)
        {
            EntityUid? container;
            Entity<SolutionComponent>? containerSoln;
            Solution? containerSolution;

            // When transferring from buffer to output bottle (filling bottle)
            if (chemMaster.Comp.SelectedBottleForFill >= 0 && chemMaster.Comp.StoredBottles[chemMaster.Comp.SelectedBottleForFill] is { } fillBottle && fromBuffer && isOutput)
            {
                container = fillBottle;
                if (!_solutionContainerSystem.TryGetSolution(container.Value, SharedChemMaster.BottleSolutionName, out containerSoln, out containerSolution))
                    return;
            }
            // When transferring from selected bottle to buffer (from bottle contents)
            else if (!fromBuffer && !isOutput && chemMaster.Comp.SelectedBottleSlot >= 0 && chemMaster.Comp.SelectedBottleSlot < chemMaster.Comp.StoredBottles.Count && chemMaster.Comp.StoredBottles[chemMaster.Comp.SelectedBottleSlot] is { } selectedBottle)
            {
                container = selectedBottle;
                if (!_solutionContainerSystem.TryGetSolution(container.Value, SharedChemMaster.BottleSolutionName, out containerSoln, out containerSolution))
                    return;
            }
            // When transferring from input container, always use input slot (ignore selected bottle)
            else if (!fromBuffer)
            {
                container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
                if (container is null ||
                    !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerEntity, out containerSolution))
                    return;
                containerSoln = containerEntity;
            }
            // When transferring from SelectedBottleForFill (legacy support) - only if fromBuffer is true
            else if (fromBuffer && chemMaster.Comp.SelectedBottleForFill >= 0 && chemMaster.Comp.StoredBottles[chemMaster.Comp.SelectedBottleForFill] is { } slotBottle)
            {
                container = slotBottle;
                if (!_solutionContainerSystem.TryGetSolution(container.Value, SharedChemMaster.BottleSolutionName, out containerSoln, out containerSolution))
                    return;
            }
            else
            {
                container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
                if (container is null ||
                    !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerEntity, out containerSolution))
                    return;
                containerSoln = containerEntity;
            }

            if (!_solutionContainerSystem.TryGetSolution((EntityUid)chemMaster, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                return;

            if (fromBuffer) // Buffer to container
            {
                var solution = bufferSolution;
                var available = solution.GetReagentQuantity(id);

                if (amount == int.MaxValue) amount = available; // Transfer all
                amount = FixedPoint2.Min(amount, available, containerSolution.AvailableVolume);
                amount = solution.RemoveReagent(id, amount, preserveOrder: true);
                _solutionContainerSystem.TryAddReagent(containerSoln.Value, id, amount, out _);
            }
            else // Container to buffer
            {
                var available = containerSolution.GetReagentQuantity(id);
                if (amount == int.MaxValue) amount = available; // Transfer all
                amount = FixedPoint2.Min(amount, available);
                if (bufferSolution.MaxVolume.Value > 0)
                    amount = FixedPoint2.Min(amount, available, bufferSolution.AvailableVolume);
                _solutionContainerSystem.RemoveReagent(containerSoln.Value, id, amount);

                var solution = bufferSolution;
                solution.AddReagent(id, amount);
            }

            UpdateUiState(chemMaster, updateLabel: true);
        }
        // ADT-Tweak-End

        // ADT-Tweak Start: discard reagents from buffer
        private void DiscardReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer, bool isOutput)
        {
            if (fromBuffer)
            {
                if (!_solutionContainerSystem.TryGetSolution((EntityUid)chemMaster, SharedChemMaster.BufferSolutionName, out var bufferEntity, out var bufferSolution))
                    return;

                var solution = bufferSolution;
                var available = solution.GetReagentQuantity(id);
                if (amount == int.MaxValue) amount = available; // Discard all
                amount = FixedPoint2.Min(amount, available);
                solution.RemoveReagent(id, amount, preserveOrder: true);
            }
            else
            {
                // ADT-Tweak Start
                EntityUid? container;
                Entity<SolutionComponent>? containerSoln;

                if (chemMaster.Comp.SelectedBottleForFill >= 0 && chemMaster.Comp.StoredBottles[chemMaster.Comp.SelectedBottleForFill] is { } bottle)
                {
                    container = bottle;
                    if (!_solutionContainerSystem.TryGetSolution(container.Value, SharedChemMaster.BottleSolutionName, out containerSoln, out var containerSolution))
                        return;
                }
                else
                {
                    container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
                    if (container is null ||
                        !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerEntity, out _))
                        return;
                    containerSoln = containerEntity;
                }


                var sol = containerSoln.Value.Comp.Solution;
                var available = sol.GetReagentQuantity(id);
                if (amount == int.MaxValue) amount = available; // Discard all
                amount = FixedPoint2.Min(amount, available);

                _solutionContainerSystem.RemoveReagent(containerSoln.Value, id, amount);
            }

            UpdateUiState(chemMaster, updateLabel: fromBuffer);
        }
        // ADT-Tweak End

        private void OnCreatePillsMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterCreatePillsMessage message)
        {
            var user = message.Actor;

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > chemMaster.Comp.PillDosageLimit)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            // ADT-Tweak Start: Use reagent amounts if available, otherwise fall back to selected reagents
            if (chemMaster.Comp.SelectedReagentAmounts.Count > 0)
            {
                // Calculate total volume for dosage calculation
                var totalVolume = FixedPoint2.Zero;
                foreach (var amount in chemMaster.Comp.SelectedReagentAmounts.Values)
                {
                    totalVolume += FixedPoint2.New(amount);
                }

                // Calculate how many pills we can create based on available volume and dosage

                var pillsToCreate = (int)message.Number;
                if (message.Dosage * message.Number > totalVolume)
                {
                    var possiblePills = (int)(totalVolume / message.Dosage);
                    pillsToCreate = Math.Min(possiblePills, (int)message.Number);
                }


                if (pillsToCreate == 0)
                    return;

                // Find available slots for storage
                var (totalAvailableSlots, availableSlots) = FindAvailablePillSlotsForCreation(chemMaster, pillsToCreate);

                // Only create pills that can fit in available slots
                var actualPillsToCreate = Math.Min(pillsToCreate, totalAvailableSlots);

                if (actualPillsToCreate == 0)
                {
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-pills-created",
                        ("created", 0), ("requested", (int)message.Number)), user);
                    return;
                }

                var totalNeeded = FixedPoint2.New(actualPillsToCreate * message.Dosage);

                var totalSelectedAmount = FixedPoint2.Zero;
                foreach (var amount in chemMaster.Comp.SelectedReagentAmounts.Values)
                {
                    totalSelectedAmount += FixedPoint2.New(amount);
                }

                if (totalSelectedAmount <= 0)
                    return;

                var scale = totalNeeded / totalSelectedAmount;

                var perPillAmounts = new Dictionary<ReagentId, FixedPoint2>();
                var proportionalAmounts = new Dictionary<ReagentId, FixedPoint2>();
                var totalForWithdrawal = FixedPoint2.Zero;
                var reagentList = chemMaster.Comp.SelectedReagentAmounts.ToList();

                // Calculate for all reagents except the last
                for (int i = 0; i < reagentList.Count - 1; i++)
                {
                    var (reagent, selectedAmount) = reagentList[i];
                    var scaledAmount = FixedPoint2.New(selectedAmount) * scale;
                    proportionalAmounts[reagent] = scaledAmount;
                    perPillAmounts[reagent] = scaledAmount / actualPillsToCreate;
                    totalForWithdrawal += scaledAmount;
                }

                // For the last reagent, use remaining to ensure exact totalNeeded
                if (reagentList.Count > 0)
                {
                    var lastReagent = reagentList[reagentList.Count - 1].Key;
                    var remaining = totalNeeded - totalForWithdrawal;
                    proportionalAmounts[lastReagent] = remaining;
                    perPillAmounts[lastReagent] = remaining / actualPillsToCreate;
                }

                // Withdraw proportional amounts from buffer
                if (!_solutionContainerSystem.TryGetSolution((EntityUid)chemMaster, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                    return;

                if (bufferSolution.Volume == 0)
                {
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), user);
                    return;
                }

                // Check if all reagents are available in sufficient quantities
                foreach (var (reagent, neededAmount) in proportionalAmounts)
                {
                    var available = bufferSolution.GetReagentQuantity(reagent);
                    if (available < neededAmount)
                    {
                        _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), user);
                        return;
                    }
                }

                // Create a solution with proportional reagent amounts
                var withdrawalSolution = new Solution();
                var withdrawnAmountsDict = new Dictionary<ReagentId, float>();

                foreach (var (reagent, neededAmount) in proportionalAmounts)
                {
                    var actualAmount = bufferSolution.RemoveReagent(reagent, neededAmount, preserveOrder: true);
                    withdrawalSolution.AddReagent(reagent, actualAmount);

                    withdrawnAmountsDict[reagent] = (int)actualAmount;
                }

                // Update selected reagent amounts after withdrawal - use actual amounts withdrawn
                UpdateSelectedReagentAmountsAfterWithdrawal(chemMaster, withdrawnAmountsDict, bufferSolution);

                var distributedPillAmounts = new Dictionary<ReagentId, FixedPoint2>();
                foreach (var reagent in proportionalAmounts.Keys)
                {
                    distributedPillAmounts[reagent] = FixedPoint2.Zero;
                }

                var pillIndex = 0;

                foreach (var (container, startSlot) in availableSlots)
                {
                    if (!TryComp(container, out StorageComponent? targetStorage))
                        continue;

                    var currentPillCount = targetStorage.Container.ContainedEntities.Count;
                    var maxPills = 10; // Standard pill canister capacity
                    var slotsAvailableInContainer = maxPills - currentPillCount;
                    var slotsToFillInContainer = Math.Min(slotsAvailableInContainer, actualPillsToCreate - pillIndex);

                    for (int slotOffset = 0; slotOffset < slotsToFillInContainer && pillIndex < actualPillsToCreate; slotOffset++)
                    {
                        var item = Spawn(PillPrototypeId, Transform(container).Coordinates);

                        var hasItemSolution = _solutionContainerSystem.EnsureSolutionEntity(
                            (item, null),
                            SharedChemMaster.PillSolutionName,
                            out var itemSolution,
                            message.Dosage);

                        if (!hasItemSolution || itemSolution is null)
                            continue;

                        // For the last pill, add any remaining amount due to rounding
                        bool isLastPill = pillIndex == actualPillsToCreate - 1;

                        // Create a new solution for this pill with exact proportional amounts
                        var pillSolution = new Solution();
                        foreach (var (reagent, amountPerPill) in perPillAmounts)
                        {
                            var amountToAdd = amountPerPill;

                            if (isLastPill)
                            {
                                // Add any remaining amount that wasn't distributed due to rounding
                                var totalDistributed = distributedPillAmounts[reagent] + amountPerPill;
                                var remaining = proportionalAmounts[reagent] - totalDistributed;
                                amountToAdd += remaining;
                            }

                            pillSolution.AddReagent(reagent, amountToAdd);
                            distributedPillAmounts[reagent] += amountToAdd;
                        }
                        pillSolution.Temperature = withdrawalSolution.Temperature;

                        _solutionContainerSystem.TryAddSolution(itemSolution.Value, pillSolution);

                        var pill = EnsureComp<PillComponent>(item);
                        pill.PillType = chemMaster.Comp.PillType;
                        Dirty(item, pill);

                        // Insert pill into the canister
                        _storageSystem.Insert(container, item, out _);
                        _labelSystem.Label(item, message.Label);

                        // Log pill creation by a user
                        _adminLogger.Add(
                            LogType.Action,
                            LogImpact.Low,
                            $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Value.Comp.Solution)}");

                        pillIndex++;
                    }
                }

                // Show message if fewer pills were created than requested
                if (actualPillsToCreate < (int)message.Number)
                {
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-pills-created",
                        ("created", actualPillsToCreate), ("requested", (int)message.Number)), user);
                }
            }
            else
            {
                // Fall back to original logic if no reagent amounts are selected
                var (totalAvailableSlots, availableSlots) = FindAvailablePillSlotsForCreation(chemMaster, (int)message.Number);

                // Only create pills that can fit in available slots
                var actualPillsToCreate = Math.Min((int)message.Number, totalAvailableSlots);

                if (actualPillsToCreate == 0)
                {
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-pills-created",
                        ("created", 0), ("requested", (int)message.Number)), user);
                    return;
                }

                // Calculate total reagent volume needed
                var totalNeeded = message.Dosage * actualPillsToCreate;

                if (!WithdrawSelectedReagentsFromBuffer(chemMaster, totalNeeded, user, out var withdrawal))
                    return;

                // Calculate per-pill amounts for each reagent to avoid rounding errors
                var perPillReagents = new Dictionary<ReagentId, FixedPoint2>();
                foreach (var (reagent, quantity) in withdrawal.Contents)
                {
                    perPillReagents[reagent] = quantity / actualPillsToCreate;
                }

                // Track how much we've actually distributed to handle rounding errors
                var distributedPillReagents = new Dictionary<ReagentId, FixedPoint2>();
                foreach (var (reagent, _) in withdrawal.Contents)
                {
                    distributedPillReagents[reagent] = FixedPoint2.Zero;
                }

                // Create pills that fit in canisters
                var pillIndex = 0;

                foreach (var (container, startSlot) in availableSlots)
                {
                    if (!TryComp(container, out StorageComponent? targetStorage))
                        continue;

                    var currentPillCount = targetStorage.Container.ContainedEntities.Count;
                    var maxPills = 10; // Standard pill canister capacity
                    var slotsAvailableInContainer = maxPills - currentPillCount;
                    var slotsToFillInContainer = Math.Min(slotsAvailableInContainer, actualPillsToCreate - pillIndex);

                    for (int slotOffset = 0; slotOffset < slotsToFillInContainer && pillIndex < actualPillsToCreate; slotOffset++)
                    {
                        var item = Spawn(PillPrototypeId, Transform(container).Coordinates);

                        var hasItemSolution = _solutionContainerSystem.EnsureSolutionEntity(
                            (item, null),
                            SharedChemMaster.PillSolutionName,
                            out var itemSolution,
                            message.Dosage);

                        if (!hasItemSolution || itemSolution is null)
                            continue;

                        // For the last pill, add any remaining amount due to rounding
                        bool isLastPill = pillIndex == actualPillsToCreate - 1;

                        // Create a new solution for this pill with exact proportional amounts
                        var pillSolution = new Solution();
                        foreach (var (reagent, amountPerPill) in perPillReagents)
                        {
                            var amountToAdd = amountPerPill;

                            if (isLastPill)
                            {
                                // Add any remaining amount that wasn't distributed due to rounding
                                var totalFromWithdrawal = withdrawal.GetReagentQuantity(reagent);
                                var totalDistributed = distributedPillReagents[reagent] + amountPerPill;
                                var remaining = totalFromWithdrawal - totalDistributed;
                                amountToAdd += remaining;
                            }

                            pillSolution.AddReagent(reagent, amountToAdd);
                            distributedPillReagents[reagent] += amountToAdd;
                        }
                        pillSolution.Temperature = withdrawal.Temperature;

                        _solutionContainerSystem.TryAddSolution(itemSolution.Value, pillSolution);

                        var pill = EnsureComp<PillComponent>(item);
                        pill.PillType = chemMaster.Comp.PillType;
                        Dirty(item, pill);

                        // Insert pill into the canister
                        _storageSystem.Insert(container, item, out _);
                        _labelSystem.Label(item, message.Label);

                        // Log pill creation by a user
                        _adminLogger.Add(
                            LogType.Action,
                            LogImpact.Low,
                            $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Value.Comp.Solution)}");

                        pillIndex++;
                    }
                }

                // Show message if fewer pills were created than requested
                if (actualPillsToCreate < (int)message.Number)
                {
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-pills-created",
                        ("created", actualPillsToCreate), ("requested", (int)message.Number)), user);
                }
            }
            // ADT-Tweak-End

            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnOutputToBottleMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterOutputToBottleMessage message)
        {
            var user = message.Actor;

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > chemMaster.Comp.BottleDosageLimit)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            // ADT-Tweak Start: Use reagent amounts if available, otherwise fall back to selected reagents
            if (chemMaster.Comp.SelectedReagentAmounts.Count > 0)
            {
                // Calculate total volume for dosage calculation
                var totalVolume = FixedPoint2.Zero;
                foreach (var amount in chemMaster.Comp.SelectedReagentAmounts.Values)
                {
                    totalVolume += FixedPoint2.New(amount);
                }

                // Calculate how many bottles we can create based on available volume and dosage
                var bottlesToCreate = (int)message.Number;
                if (message.Dosage * message.Number > totalVolume)
                {
                    var possibleBottles = (int)(totalVolume / message.Dosage);
                    bottlesToCreate = Math.Min(possibleBottles, (int)message.Number);
                }

                if (bottlesToCreate == 0)
                    return;

                // Find available empty bottles using the same logic as pills
                // If a specific bottle is selected, only fill that one. Otherwise, fill all empty bottles.
                var targets = FindAvailableBottlesForFilling(chemMaster, bottlesToCreate, message.Dosage);

                // Only create bottles that can fit in available slots
                var actualBottlesToCreate = Math.Min(bottlesToCreate, targets.Count);

                if (actualBottlesToCreate == 0)
                {
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-bottles-created",
                        ("created", 0), ("requested", (int)message.Number)), user);
                    return;
                }

                // Calculate total amount needed based on actualBottlesToCreate and message.Dosage
                // This is the exact amount that will be removed from buffer and distributed among bottles
                var totalNeeded = FixedPoint2.New(actualBottlesToCreate * message.Dosage);

                // Calculate total selected reagent amount to determine proportions
                var totalSelectedAmount = FixedPoint2.Zero;
                foreach (var amount in chemMaster.Comp.SelectedReagentAmounts.Values)
                {
                    totalSelectedAmount += FixedPoint2.New(amount);
                }

                if (totalSelectedAmount <= 0)
                    return;

                // Scale selected amounts to match totalNeeded
                var scale = totalNeeded / totalSelectedAmount;

                // Calculate amount to withdraw for each reagent and amount per bottle
                var perBottleAmounts = new Dictionary<ReagentId, FixedPoint2>();
                var proportionalAmounts = new Dictionary<ReagentId, FixedPoint2>();
                var totalForWithdrawal = FixedPoint2.Zero;
                var reagentList = chemMaster.Comp.SelectedReagentAmounts.ToList();

                // Calculate for all reagents except the last
                for (int i = 0; i < reagentList.Count - 1; i++)
                {
                    var (reagent, selectedAmount) = reagentList[i];
                    var scaledAmount = FixedPoint2.New(selectedAmount) * scale;
                    proportionalAmounts[reagent] = scaledAmount;
                    perBottleAmounts[reagent] = scaledAmount / actualBottlesToCreate;
                    totalForWithdrawal += scaledAmount;
                }

                // For the last reagent, use remaining to ensure exact totalNeeded
                if (reagentList.Count > 0)
                {
                    var lastReagent = reagentList[reagentList.Count - 1].Key;
                    var remaining = totalNeeded - totalForWithdrawal;
                    proportionalAmounts[lastReagent] = remaining;
                    perBottleAmounts[lastReagent] = remaining / actualBottlesToCreate;
                }

                // Withdraw proportional amounts from buffer
                if (!_solutionContainerSystem.TryGetSolution((EntityUid)chemMaster, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                    return;

                if (bufferSolution.Volume == 0)
                {
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), user);
                    return;
                }

                // Check if all reagents are available in sufficient quantities
                foreach (var (reagent, neededAmount) in proportionalAmounts)
                {
                    var available = bufferSolution.GetReagentQuantity(reagent);
                    if (available < neededAmount)
                    {
                        _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), user);
                        return;
                    }
                }

                // Create a solution with proportional reagent amounts
                var withdrawalSolution = new Solution();
                var withdrawnAmountsDict = new Dictionary<ReagentId, float>();

                foreach (var (reagent, neededAmount) in proportionalAmounts)
                {
                    // Remove reagent from buffer - this returns the actual amount removed
                    var actualAmount = bufferSolution.RemoveReagent(reagent, neededAmount, preserveOrder: true);
                    withdrawalSolution.AddReagent(reagent, actualAmount);

                    // Store the actual amount withdrawn (not the requested amount) for updating SelectedReagentAmounts
                    withdrawnAmountsDict[reagent] = (int)actualAmount;
                }

                // Update selected reagent amounts after withdrawal - use actual amounts withdrawn
                UpdateSelectedReagentAmountsAfterWithdrawal(chemMaster, withdrawnAmountsDict, bufferSolution);

                // Use perBottleAmounts calculated earlier - each bottle gets exact dosage distributed proportionally
                // Track how much we've actually distributed to handle rounding errors
                var distributedAmounts = new Dictionary<ReagentId, FixedPoint2>();
                foreach (var reagent in proportionalAmounts.Keys)
                {
                    distributedAmounts[reagent] = FixedPoint2.Zero;
                }

                for (int i = 0; i < actualBottlesToCreate; i++)
                {
                    var bottle = targets[i];
                    if (!_solutionContainerSystem.TryGetSolution(bottle, SharedChemMaster.BottleSolutionName, out var soln, out var solution))
                        continue;

                    if (message.Dosage > solution.AvailableVolume)
                        continue;

                    // Create a new solution for this bottle with exact proportional amounts
                    var bottleSolution = new Solution();
                    // For the last bottle, add any remaining amount due to rounding
                    bool isLastBottle = i == actualBottlesToCreate - 1;

                    // Track actual amounts added for label
                    var actualAmountsInBottle = new Dictionary<ReagentId, FixedPoint2>();

                    foreach (var (reagent, amountPerBottle) in perBottleAmounts)
                    {
                        var amountToAdd = amountPerBottle;

                        if (isLastBottle)
                        {
                            // Add any remaining amount that wasn't distributed due to rounding
                            var totalDistributed = distributedAmounts[reagent] + amountPerBottle;
                            var remaining = proportionalAmounts[reagent] - totalDistributed;
                            amountToAdd += remaining;
                        }

                        bottleSolution.AddReagent(reagent, amountToAdd);
                        distributedAmounts[reagent] += amountToAdd;
                        actualAmountsInBottle[reagent] = amountToAdd;
                    }
                    bottleSolution.Temperature = withdrawalSolution.Temperature;

                    _solutionContainerSystem.TryAddSolution(soln.Value, bottleSolution);

                    // Format and apply label
                    var label = FormatReagentLabel(actualAmountsInBottle, message.Label);
                    _labelSystem.Label(bottle, label);

                    // Log bottle fill by a user
                    _adminLogger.Add(LogType.Action, LogImpact.Low,
                        $"{ToPrettyString(user):user} bottled {ToPrettyString(bottle):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
                }

                // Show message if fewer bottles were created than requested
                if (actualBottlesToCreate < (int)message.Number)
                {
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-bottles-created",
                        ("created", actualBottlesToCreate), ("requested", (int)message.Number)), user);
                }
            }
            else
            {
                // Find available empty bottles using the same logic as pills
                // If a specific bottle is selected, only fill that one. Otherwise, fill all empty bottles.
                var targets = FindAvailableBottlesForFilling(chemMaster, (int)message.Number, message.Dosage);

                // Only create bottles that can fit in available slots
                var actualBottlesToCreate = Math.Min((int)message.Number, targets.Count);

                if (actualBottlesToCreate == 0)
                {
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-bottles-created",
                        ("created", 0), ("requested", (int)message.Number)), user);
                    return;
                }

                // Calculate total reagent volume needed based on actual bottles we can create
                var needed = message.Dosage * actualBottlesToCreate;

                if (!WithdrawSelectedReagentsFromBuffer(chemMaster, needed, user, out var withdrawal))
                    return;

                // Calculate per-bottle amounts for each reagent to avoid rounding errors
                var perBottleReagents = new Dictionary<ReagentId, FixedPoint2>();
                foreach (var (reagent, quantity) in withdrawal.Contents)
                {
                    perBottleReagents[reagent] = quantity / actualBottlesToCreate;
                }

                // Track how much we've actually distributed to handle rounding errors
                var distributedReagents = new Dictionary<ReagentId, FixedPoint2>();
                foreach (var (reagent, _) in withdrawal.Contents)
                {
                    distributedReagents[reagent] = FixedPoint2.Zero;
                }

                for (int i = 0; i < actualBottlesToCreate; i++)
                {
                    var bottle = targets[i];
                    if (!_solutionContainerSystem.TryGetSolution(bottle, SharedChemMaster.BottleSolutionName, out var soln, out var solution))
                        continue;

                    if (message.Dosage > solution.AvailableVolume)
                        continue;

                    // Create a new solution for this bottle with exact proportional amounts
                    var bottleSolution = new Solution();
                    // For the last bottle, add any remaining amount due to rounding
                    bool isLastBottle = (i == actualBottlesToCreate - 1);

                    // Track actual amounts added for label
                    var actualAmountsInBottle = new Dictionary<ReagentId, FixedPoint2>();

                    foreach (var (reagent, amountPerBottle) in perBottleReagents)
                    {
                        var amountToAdd = amountPerBottle;

                        if (isLastBottle)
                        {
                            // Add any remaining amount that wasn't distributed due to rounding
                            var totalFromWithdrawal = withdrawal.GetReagentQuantity(reagent);
                            var totalDistributed = distributedReagents[reagent] + amountPerBottle;
                            var remaining = totalFromWithdrawal - totalDistributed;
                            amountToAdd += remaining;
                        }

                        bottleSolution.AddReagent(reagent, amountToAdd);
                        distributedReagents[reagent] += amountToAdd;
                        actualAmountsInBottle[reagent] = amountToAdd;
                    }
                    bottleSolution.Temperature = withdrawal.Temperature;

                    _solutionContainerSystem.TryAddSolution(soln.Value, bottleSolution);

                    // Format and apply label
                    var label = FormatReagentLabel(actualAmountsInBottle, message.Label);
                    _labelSystem.Label(bottle, label);

                    // Log bottle fill by a user
                    _adminLogger.Add(LogType.Action, LogImpact.Low,
                        $"{ToPrettyString(user):user} bottled {ToPrettyString(bottle):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
                }

                // Show message if fewer bottles were created than requested
                if (actualBottlesToCreate < (int)message.Number)
                {
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-bottles-created",
                        ("created", actualBottlesToCreate), ("requested", (int)message.Number)), user);
                }
            }
            // ADT-Tweak End
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private bool WithdrawSelectedReagentsFromBuffer(
            Entity<ChemMasterComponent> chemMaster,
            FixedPoint2 neededVolume, EntityUid? user,
            [NotNullWhen(returnValue: true)] out Solution? outputSolution)
        {
            outputSolution = null;

            if (!_solutionContainerSystem.TryGetSolution((EntityUid)chemMaster, SharedChemMaster.BufferSolutionName, out _, out var solution))
                return false;

            if (solution.Volume == 0)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), user.Value);
                return false;
            }

            // Get selected reagents for creation
            var selectedReagents = chemMaster.Comp.SelectedReagents;

            // ADT-Tweak Start: Check if any reagents are selected
            if (selectedReagents.Count == 0)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-no-reagent-selected-text"), user.Value);
                return false;
            }

            // Filter to only reagents that actually exist in the buffer
            var availableReagents = new List<ReagentId>();
            var totalAvailableFromSelected = FixedPoint2.Zero;
            foreach (var reagent in selectedReagents)
            {
                var quantity = solution.GetReagentQuantity(reagent);
                if (quantity > 0)
                {
                    availableReagents.Add(reagent);
                    totalAvailableFromSelected += quantity;
                }
            }

            if (availableReagents.Count == 0)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-selected-reagent-not-found-text"), user.Value);
                return false;
            }

            // Check if total available from selected reagents is enough
            if (totalAvailableFromSelected < neededVolume)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), user.Value);
                return false;
            }

            // Use the total needed volume
            var totalNeededVolume = neededVolume;

            // Create a new solution with the selected reagents
            outputSolution = new Solution();

            // Withdraw from selected reagents proportionally or in order until total needed is reached
            var remainingNeeded = totalNeededVolume;
            foreach (var reagent in availableReagents)
            {
                if (remainingNeeded <= 0) break;
                var available = solution.GetReagentQuantity(reagent);
                var takeAmount = FixedPoint2.Min(available, remainingNeeded);
                var actualAmount = solution.RemoveReagent(reagent, takeAmount, preserveOrder: true);
                outputSolution.AddReagent(reagent, actualAmount);
                remainingNeeded -= actualAmount;
            }

            return true;
            // ADT-Tweak End
        }

        // ADT-Tweak Start: Update Selected Reagents or clamp it
        private void UpdateSelectedReagentAmountsAfterWithdrawal(
            Entity<ChemMasterComponent> chemMaster,
            Dictionary<ReagentId, float> withdrawnAmounts,
            Solution bufferSolution)
        {
            // Create a new dictionary for updated amounts
            var updatedAmounts = new Dictionary<ReagentId, float>();

            foreach (var (reagent, selectedAmount) in chemMaster.Comp.SelectedReagentAmounts)
            {
                // Check how much was actually withdrawn
                var withdrawn = withdrawnAmounts.GetValueOrDefault(reagent, 0);
                var remainingSelected = selectedAmount - withdrawn;

                // Check how much is actually available in buffer now
                var availableInBuffer = (float)bufferSolution.GetReagentQuantity(reagent);

                // Selected amount should not exceed what's available in buffer
                var newSelectedAmount = Math.Min(remainingSelected, availableInBuffer);

                if (newSelectedAmount > 0)
                {
                    updatedAmounts[reagent] = newSelectedAmount;
                }
            }

            chemMaster.Comp.SelectedReagentAmounts = updatedAmounts;
        }

        private void CorrectSelectedReagentAmounts(ChemMasterComponent chemMaster, Solution bufferSolution)
        {
            var updatedAmounts = new Dictionary<ReagentId, float>();

            foreach (var (reagent, selectedAmount) in chemMaster.SelectedReagentAmounts)
            {
                // Check how much is actually available in buffer
                var availableInBuffer = (float)bufferSolution.GetReagentQuantity(reagent);

                // Only keep reagent in selection if there's both a selected amount and available amount
                if (selectedAmount > 0 && availableInBuffer > 0)
                {
                    updatedAmounts[reagent] = Math.Min(selectedAmount, availableInBuffer);
                }
                // If availableInBuffer is 0, don't add to updatedAmounts - effectively removing it
            }

            chemMaster.SelectedReagentAmounts = updatedAmounts;
        }

        private string FormatReagentLabel(Dictionary<ReagentId, FixedPoint2> reagents, string baseLabel = "")
        {
            var parts = new List<string>();

            foreach (var (reagentId, amount) in reagents)
            {
                // Try to get reagent prototype for the name
                if (_prototypeManager.TryIndex<ReagentPrototype>(reagentId.Prototype, out var reagentProto))
                {
                    var reagentName = Loc.GetString(reagentProto.LocalizedName);
                    parts.Add($"{reagentName}:{amount:F0}u");
                }
            }

            var compositionLabel = string.Join("/", parts);

            if (!string.IsNullOrWhiteSpace(baseLabel))
            {
                return $"{baseLabel} ({compositionLabel})";
            }

            return compositionLabel;
        }
        // ADT-Tweak End

        private void ClickSound(Entity<ChemMasterComponent> chemMaster)
        {
            _audioSystem.PlayPvs(chemMaster.Comp.ClickSound, chemMaster, AudioParams.Default.WithVolume(-2f));
        }

        private ContainerInfo? BuildInputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (!TryComp(container, out FitsInDispenserComponent? fits)
                || !_solutionContainerSystem.TryGetSolution(container.Value, fits.Solution, out _, out var solution))
            {
                return null;
            }

            return BuildContainerInfo(Name(container.Value), solution);
        }

        // ADT-Tweak-Start: Update sorting methods, build Container info for UI update and Input solution
        private static ContainerInfo BuildContainerInfo(string name, Solution solution) =>
            new(name, solution.Volume, solution.MaxVolume)
            {
                Reagents = solution.Contents
            };

        private void OnSortMethodUpdated(EntityUid uid, ChemMasterComponent chemMaster, ChemMasterSortMethodUpdated args)
        {
            chemMaster.SortMethod = args.SortMethod;
            UpdateUiState((uid, chemMaster));
        }

        private void OnTransferringAmountUpdated(EntityUid uid,
            ChemMasterComponent chemMaster,
            ChemMasterTransferringAmountUpdated args)
        {
            chemMaster.TransferringAmount = args.TransferringAmount;
            ClickSound((uid, chemMaster));
            UpdateUiState((uid, chemMaster));
        }
        // ADT-Tweak End

        // ADT-Tweak Start: Bottle buttons reagent transferring
        private void OnSelectBottleSlotMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSelectBottleSlotMessage message)
        {
            if (message.Slot < 0 || message.Slot >= chemMaster.Comp.StoredBottles.Count)
                return;

            chemMaster.Comp.SelectedBottleSlot = message.Slot;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }


        private void OnChooseReagentMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterChooseReagentMessage message)
        {
            chemMaster.Comp.SelectedReagent = message.Reagent;
            UpdateUiState(chemMaster);
        }

        private void OnClearReagentSelectionMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterClearReagentSelectionMessage message)
        {
            chemMaster.Comp.SelectedReagent = null;
            UpdateUiState(chemMaster);
        }

        private void OnToggleBottleFillMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterToggleBottleFillMessage message)
        {
            if (message.Slot < 0 || message.Slot >= chemMaster.Comp.StoredBottles.Count || chemMaster.Comp.StoredBottles[message.Slot] == null)
                return;

            if (chemMaster.Comp.SelectedBottleForFill == message.Slot)
            {
                chemMaster.Comp.SelectedBottleForFill = -1;
            }
            else
            {
                chemMaster.Comp.SelectedBottleForFill = message.Slot;
            }
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnRowEjectMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterRowEjectMessage message)
        {
            var row = message.Row;
            var startSlot = row * 4;
            var endSlot = startSlot + 3;
            for (int slot = startSlot; slot <= endSlot; slot++)
            {
                if (slot >= chemMaster.Comp.StoredBottles.Count)
                    continue;
                var slotName = $"bottleSlot{slot}";
                if (_itemSlotsSystem.TryGetSlot((EntityUid)chemMaster, slotName, out var itemSlot))
                    _itemSlotsSystem.TryEject((EntityUid)chemMaster, itemSlot, message.Actor, out _, excludeUserAudio: true);
            }
            UpdateUiState(chemMaster);
        }
        // ADT-Tweak End

        // ADT-Tweak Start: Initializing containers
        private void OnMapInit(EntityUid uid, ChemMasterComponent component, MapInitEvent args)
        {
            // Pill container slots (3 slots for pill containers, 3x10 grid)
            for (int i = 0; i < 3; i++)
            {
                var slotId = "pillContainerSlot" + i;
                ItemSlot slot = new();
                var whitelist = new EntityWhitelist();
                whitelist.Tags = new List<ProtoId<TagPrototype>> { "PillCanister" };
                slot.Whitelist = whitelist;
                _itemSlotsSystem.AddItemSlot(uid, slotId, slot);
            }

            // Bottle container slots (20 slots for bottles, 4x5 grid)
            for (int i = 0; i < 20; i++)
            {
                var slotId = "bottleSlot" + i;
                ItemSlot slot = new();
                var whitelist = new EntityWhitelist();
                whitelist.Tags = new List<ProtoId<TagPrototype>> { "Bottle" };
                slot.Whitelist = whitelist;
                _itemSlotsSystem.AddItemSlot(uid, slotId, slot);
            }
        }



        // ADT-Tweak Start: Pill container message handlers
        private void OnSelectPillContainerSlotMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSelectPillContainerSlotMessage message)
        {
            if (message.Slot < 0 || message.Slot >= 30) // 3 containers × 10 slots each
                return;

            chemMaster.Comp.SelectedPillContainerSlot = message.Slot;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnTogglePillContainerFillMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterTogglePillContainerFillMessage message)
        {
            if (message.Slot < 0 || message.Slot >= 30) // 3 containers × 10 slots each
                return;

            if (chemMaster.Comp.SelectedPillContainerForFill == message.Slot)
            {
                chemMaster.Comp.SelectedPillContainerForFill = -1;
            }
            else
            {
                chemMaster.Comp.SelectedPillContainerForFill = message.Slot;
            }
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnPillContainerSlotEjectMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterPillContainerSlotEjectMessage message)
        {
            if (message.Slot < 0 || message.Slot >= 30) // 3 containers × 10 slots each
                return;

            // Calculate which container and slot this refers to
            var containerIndex = message.Slot / 10;
            var slotIndex = message.Slot % 10;

            if (containerIndex < chemMaster.Comp.StoredPillContainers.Count &&
                chemMaster.Comp.StoredPillContainers[containerIndex] is { } pillContainer)
            {
                // Try to eject a pill from the specific slot
                if (TryComp(pillContainer, out StorageComponent? storage) &&
                    storage.Container.ContainedEntities.Count > slotIndex)
                {
                    var pillToEject = storage.Container.ContainedEntities.ElementAt(slotIndex);
                    _storageSystem.Insert(pillContainer, pillToEject, out _, user: chemMaster);
                }
            }

            UpdateUiState(chemMaster);
        }

        private void OnPillContainerRowEjectMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterPillContainerRowEjectMessage message)
        {
            if (message.Row < 0 || message.Row >= 3)
                return;

            var startSlot = message.Row * 10;
            var endSlot = startSlot + 9;

            for (int slot = startSlot; slot <= endSlot; slot++)
            {
                if (slot >= 30) // Safety check
                    continue;

                // Calculate which container this slot belongs to
                var containerIndex = slot / 10;

                if (containerIndex < chemMaster.Comp.StoredPillContainers.Count &&
                    chemMaster.Comp.StoredPillContainers[containerIndex] is { } pillContainer)
                {
                    if (TryComp(pillContainer, out StorageComponent? storage) &&
                        storage.Container.ContainedEntities.Count > (slot % 10))
                    {
                        var pillToEject = storage.Container.ContainedEntities.ElementAt(slot % 10);
                        _storageSystem.Insert(pillContainer, pillToEject, out _, user: chemMaster);
                    }
                }
            }

            UpdateUiState(chemMaster);
        }

        private void OnSelectPillCanisterForCreationMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSelectPillCanisterForCreationMessage message)
        {
            if (message.CanisterIndex < 0 || message.CanisterIndex >= 3)
                return;

            // Toggle: if the same canister is selected, deselect it
            if (chemMaster.Comp.SelectedPillCanisterForCreation == message.CanisterIndex)
            {
                chemMaster.Comp.SelectedPillCanisterForCreation = -1;
            }
            else
            {
                chemMaster.Comp.SelectedPillCanisterForCreation = message.CanisterIndex;
            }

            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnItemSlotButtonPressed(Entity<ChemMasterComponent> chemMaster, ref ItemSlotButtonPressedEvent message)
        {
            if (message.SlotId.StartsWith("pillContainerSlot") && int.TryParse(message.SlotId.Replace("pillContainerSlot", ""), out int canisterIndex) && canisterIndex >= 0 && canisterIndex < 3)
            {
                var slotId = $"pillContainerSlot{canisterIndex}";
                if (_itemSlotsSystem.TryGetSlot(chemMaster, slotId, out var slot) && slot.Item.HasValue)
                {
                    _itemSlotsSystem.TryEject((EntityUid)chemMaster, slot, message.Actor, out _, excludeUserAudio: true);
                }

                UpdateUiState(chemMaster);
            }
        }
        // ADT-Tweak End

        // ADT-Tweak: Reagent amount selection handlers
        private void OnSelectReagentAmountMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSelectReagentAmountMessage message)
        {
            // Validate: check if the user is trying to select more than available in buffer
            if (!_solutionContainerSystem.TryGetSolution((EntityUid)chemMaster, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                return;

            var availableInBuffer = bufferSolution.GetReagentQuantity(message.Reagent);

            // Get currently selected amount for this reagent
            float currentlySelected = 0;
            if (chemMaster.Comp.SelectedReagentAmounts.TryGetValue(message.Reagent, out var selectedAmount))
            {
                currentlySelected = selectedAmount;
            }
            else
            {
                // Try finding by prototype
                foreach (var kvp in chemMaster.Comp.SelectedReagentAmounts)
                {
                    if (kvp.Key.Prototype == message.Reagent.Prototype)
                    {
                        currentlySelected = kvp.Value;
                        break;
                    }
                }
            }

            // Add the amount to existing value (accumulate)
            // First try direct lookup
            if (chemMaster.Comp.SelectedReagentAmounts.TryGetValue(message.Reagent, out var currentAmount))
            {
                chemMaster.Comp.SelectedReagentAmounts[message.Reagent] = currentAmount + message.Amount;
                CorrectSelectedReagentAmounts(chemMaster, bufferSolution);
            }
            else
            {
                // If direct lookup fails, try comparing by Prototype string
                // This handles cases where ReagentId instances might have different Data but same Prototype
                ReagentId? existingKey = null;
                float existingAmount = 0;

                foreach (var kvp in chemMaster.Comp.SelectedReagentAmounts)
                {
                    if (kvp.Key.Prototype == message.Reagent.Prototype)
                    {
                        existingKey = kvp.Key;
                        existingAmount = kvp.Value;
                        break;
                    }
                }

                if (existingKey != null)
                {
                    if (currentlySelected + message.Amount > availableInBuffer)
                    {
                        chemMaster.Comp.SelectedReagentAmounts[existingKey.Value] = (float)availableInBuffer;
                    }
                    else
                    {
                        // Found existing entry by prototype - update it
                        chemMaster.Comp.SelectedReagentAmounts[existingKey.Value] = existingAmount + message.Amount;
                    }

                }
                else
                {
                    if (!(currentlySelected + message.Amount > availableInBuffer))
                    {
                        // No existing entry - create new
                        chemMaster.Comp.SelectedReagentAmounts[message.Reagent] = message.Amount;
                    }
                    else
                    {
                        chemMaster.Comp.SelectedReagentAmounts[message.Reagent] = (float)availableInBuffer;
                    }
                }
            }

            UpdateUiState(chemMaster, updateLabel: true);
            ClickSound(chemMaster);
        }

        private void OnRemoveReagentAmountMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterRemoveReagentAmountMessage message)
        {
            // First try direct lookup
            if (chemMaster.Comp.SelectedReagentAmounts.TryGetValue(message.Reagent, out var currentAmount))
            {
                var newAmount = Math.Max(0, currentAmount - message.Amount);
                if (newAmount == 0)
                {
                    chemMaster.Comp.SelectedReagentAmounts.Remove(message.Reagent);
                }
                else
                {
                    chemMaster.Comp.SelectedReagentAmounts[message.Reagent] = newAmount;
                }
            }
            else
            {
                // If direct lookup fails, try comparing by Prototype string
                ReagentId? existingKey = null;
                float existingAmount = 0;

                foreach (var kvp in chemMaster.Comp.SelectedReagentAmounts)
                {
                    if (kvp.Key.Prototype == message.Reagent.Prototype)
                    {
                        existingKey = kvp.Key;
                        existingAmount = kvp.Value;
                        break;
                    }
                }

                if (existingKey != null)
                {
                    var newAmount = Math.Max(0, existingAmount - message.Amount);
                    if (newAmount == 0)
                    {
                        chemMaster.Comp.SelectedReagentAmounts.Remove(existingKey.Value);
                    }
                    else
                    {
                        chemMaster.Comp.SelectedReagentAmounts[existingKey.Value] = newAmount;
                    }
                }
            }

            UpdateUiState(chemMaster, updateLabel: true);
            ClickSound(chemMaster);
        }

        private void OnClearReagentAmountMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterClearReagentAmountMessage message)
        {
            // First try direct removal
            if (!chemMaster.Comp.SelectedReagentAmounts.Remove(message.Reagent))
            {
                // If direct removal fails, try comparing by Prototype string
                ReagentId? existingKey = null;

                foreach (var kvp in chemMaster.Comp.SelectedReagentAmounts)
                {
                    if (kvp.Key.Prototype == message.Reagent.Prototype)
                    {
                        existingKey = kvp.Key;
                        break;
                    }
                }

                if (existingKey != null)
                {
                    chemMaster.Comp.SelectedReagentAmounts.Remove(existingKey.Value);
                }
            }

            UpdateUiState(chemMaster, updateLabel: true);
            ClickSound(chemMaster);
        }
        // ADT-Tweak End
    }
}
