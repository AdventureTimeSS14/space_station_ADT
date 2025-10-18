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
using System.Collections.Generic;
using Content.Shared.Whitelist;
using Content.Shared.Tag;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Storage.Events;
using Content.Shared.Storage.Components;
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
        [Dependency] private readonly IEntityManager _entityManager = default!; //ADT-Tweak

        private static readonly EntProtoId PillPrototypeId = "Pill";

        [ValidatePrototypeId<EntityPrototype>]
        private const string PillCanisterPrototypeId = "PillCanister";  //ADT-Tweak

        // Prevent recursion while repacking bottle slots after insert/remove.
        private readonly HashSet<EntityUid> _packing = new();
        // Suppress UI churn during input-slot bottle relocation.
        private readonly HashSet<EntityUid> _relocating = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChemMasterComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, EntInsertedIntoContainerMessage>(OnContainerInserted); //ADT-Tweak
            SubscribeLocalEvent<ChemMasterComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);   //ADT-Tweak
            SubscribeLocalEvent<ChemMasterComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetModeMessage>(OnSetModeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetPillTypeMessage>(OnSetPillTypeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterReagentAmountButtonMessage>(OnReagentButtonMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterCreatePillsMessage>(OnCreatePillsMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterOutputToBottleMessage>(OnOutputToBottleMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSortMethodUpdated>(OnSortMethodUpdated);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterTransferringAmountUpdated>(OnTransferringAmountUpdated);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterAmountsUpdated>(OnAmountsUpdated);
            // ADT-Tweak Start
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSelectBottleSlotMessage>(OnSelectBottleSlotMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterChooseReagentMessage>(OnChooseReagentMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterClearReagentSelectionMessage>(OnClearReagentSelectionMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterReagentToggledOnMessage>(OnReagentToggledOnMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterReagentToggledOffMessage>(OnReagentToggledOffMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterToggleBottleFillMessage>(OnToggleBottleFillMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterRowEjectMessage>(OnRowEjectMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSelectPillContainerSlotMessage>(OnSelectPillContainerSlotMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterTogglePillContainerFillMessage>(OnTogglePillContainerFillMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterPillContainerSlotEjectMessage>(OnPillContainerSlotEjectMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterPillContainerRowEjectMessage>(OnPillContainerRowEjectMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSelectPillCanisterForCreationMessage>(OnSelectPillCanisterForCreationMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSelectReagentAmountMessage>(OnSelectReagentAmountMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterRemoveReagentAmountMessage>(OnRemoveReagentAmountMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterClearReagentAmountMessage>(OnClearReagentAmountMessage);
            SubscribeLocalEvent<ChemMasterComponent, ItemSlotButtonPressedEvent>(OnItemSlotButtonPressed);
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

            // Initialize stored pill containers list with correct size if needed (3 containers)
            if (chemMaster.StoredPillContainers.Count != 3)
            {
                chemMaster.StoredPillContainers.Clear();
                for (int i = 0; i < 3; i++)
                    chemMaster.StoredPillContainers.Add(null);
            }

            // Initialize stored bottles list with correct size if needed (legacy support)
            if (chemMaster.StoredBottles.Count != 20)
            {
                chemMaster.StoredBottles.Clear();
                for (int i = 0; i < 20; i++)
                    chemMaster.StoredBottles.Add(null);
            }
            //ADT-Tweak End

            var container = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.InputSlotName);

            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.Volume;

            // ADT-Tweak: Pill container storage
            var storedPillContainersInfo = new List<ContainerInfo?>();
            var pillContainers = new List<List<bool>>();
            var pillTypes = new List<List<uint>>(); // ADT-Tweak: Pill types for each slot in each container
            chemMaster.StoredPillContainers.Clear();

            for (int i = 0; i < 3; i++)
            {
                var slotId = "pillContainerSlot" + i;

                if (_itemSlotsSystem.TryGetSlot(chemMaster.Owner, slotId, out var slot) && slot.Item.HasValue)
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

            // ADT-Tweak: Legacy bottle storage
            var storedBottlesInfo = new List<ContainerInfo?>();
            chemMaster.StoredBottles.Clear();

            for (int i = 0; i < 20; i++)
            {
                var slotId = "bottleSlot" + i;

                if (_itemSlotsSystem.TryGetSlot(chemMaster.Owner, slotId, out var slot) && slot.Item.HasValue)
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

            // ADT-Tweak Start
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
                pillTypes, // ADT-Tweak: Include pill types information
                chemMaster.SelectedPillContainerSlot,
                chemMaster.SelectedPillContainerForFill,
                chemMaster.SelectedPillCanisterForCreation,
                chemMaster.SelectedReagent,
                storedBottlesInfo,
                chemMaster.SelectedBottleSlot,
                chemMaster.SelectedBottleForFill,
                chemMaster.SelectedReagentsForBottles,
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
                    var owner = chemMaster.Owner;
                    var addedGuard = _relocating.Add(owner);

                    try
                    {
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
                    finally
                    {
                        if (addedGuard)
                            _relocating.Remove(owner);
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
            if (_relocating.Contains(chemMaster.Owner))
                return;
            var owner = chemMaster.Owner;
            // Determine which slot triggered the removal by container ID.
            // Only repack if the removal came from one of the bottle grid slots (bottleSlot0..19).
            var containerId = args.Container?.ID;
            var removedFromBottleSlot = containerId != null && containerId.StartsWith("bottleSlot");

            if (removedFromBottleSlot)
            {
                var removeId = "bottleSlot" + containerId;
                if (_itemSlotsSystem.TryGetSlot(owner, removeId, out var slot) && slot.Item.HasValue)
                    _itemSlotsSystem.TryEject(owner, slot, null, out _, excludeUserAudio: true);
            }

            // Always refresh UI (covers beaker removal and any other container content changes).
            UpdateUiState(chemMaster);
        }

        /// <summary>
        /// Checks if the given storage entity is one of our pill containers.
        /// </summary>
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

        /// <summary>
        /// Finds available empty slots for pill creation, either in the selected canister or all canisters.
        /// Returns the total available slots and a list of (container, startSlot) pairs for filling.
        /// </summary>
        private (int totalAvailableSlots, List<(EntityUid container, int startSlot)> availableSlots) FindAvailablePillSlotsForCreation(Entity<ChemMasterComponent> chemMaster, int requestedPills)
        {
            var availableSlots = new List<(EntityUid, int)>();
            var totalAvailableSlots = 0;

            // If a specific canister is selected, only use that one
            if (chemMaster.Comp.SelectedPillCanisterForCreation >= 0 && chemMaster.Comp.SelectedPillCanisterForCreation < 3)
            {
                var selectedContainerIndex = chemMaster.Comp.SelectedPillCanisterForCreation;
                var slotId = "pillContainerSlot" + selectedContainerIndex;

                if (_itemSlotsSystem.TryGetSlot(chemMaster.Owner, slotId, out var slot) && slot.Item.HasValue)
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

                    if (_itemSlotsSystem.TryGetSlot(chemMaster.Owner, slotId, out var slot) && slot.Item.HasValue)
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

        /// <summary>
        /// Finds the first empty slot in any pill canister, prioritizing the selected canister.
        /// Returns the container and slot index, or null if no empty slots found.
        /// Kept for backward compatibility.
        /// </summary>
        private (EntityUid container, int slotIndex)? FindEmptyPillSlotPrioritizingSelected(Entity<ChemMasterComponent> chemMaster)
        {
            // First, try to use the selected pill canister if one is selected
            if (chemMaster.Comp.SelectedPillCanisterForCreation >= 0 && chemMaster.Comp.SelectedPillCanisterForCreation < 3)
            {
                var selectedContainerIndex = chemMaster.Comp.SelectedPillCanisterForCreation;
                var slotId = "pillContainerSlot" + selectedContainerIndex;

                if (_itemSlotsSystem.TryGetSlot(chemMaster.Owner, slotId, out var slot) && slot.Item.HasValue)
                {
                    var pillContainer = slot.Item.Value;

                    if (TryComp(pillContainer, out StorageComponent? storage))
                    {
                        var currentPillCount = storage.Container.ContainedEntities.Count;
                        var maxPills = 10; // Standard pill canister capacity

                        // Check if there's an empty slot in the selected container
                        for (int slotIndex = 0; slotIndex < maxPills; slotIndex++)
                        {
                            if (slotIndex >= currentPillCount)
                            {
                                // Found an empty slot in the selected container
                                return (pillContainer, slotIndex);
                            }
                        }
                    }
                }
            }

            // If no selected canister or selected canister is full, fall back to any available slot
            return FindFirstEmptyPillSlot(chemMaster);
        }

        /// <summary>
        /// Finds the first empty slot in any pill canister.
        /// Returns the container and slot index, or null if no empty slots found.
        /// </summary>
        private (EntityUid container, int slotIndex)? FindFirstEmptyPillSlot(Entity<ChemMasterComponent> chemMaster)
        {
            // Check all 3 pill container slots
            for (int containerIndex = 0; containerIndex < 3; containerIndex++)
            {
                var slotId = "pillContainerSlot" + containerIndex;

                if (_itemSlotsSystem.TryGetSlot(chemMaster.Owner, slotId, out var slot) && slot.Item.HasValue)
                {
                    var pillContainer = slot.Item.Value;

                    if (TryComp(pillContainer, out StorageComponent? storage))
                    {
                        var currentPillCount = storage.Container.ContainedEntities.Count;
                        var maxPills = 10; // Standard pill canister capacity

                        // Check if there's an empty slot in this container
                        for (int slotIndex = 0; slotIndex < maxPills; slotIndex++)
                        {
                            if (slotIndex >= currentPillCount)
                            {
                                // Found an empty slot
                                return (pillContainer, slotIndex);
                            }
                        }
                    }
                }
            }

            // No empty slots found in any pill canister
            return null;
        }
        // ADT-Tweak End

        // ADT-Tweak-Start: Additional logic for second (pill) buffer
        private void TransferReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer, bool isOutput)
        {
            EntityUid? container = null;
            Entity<SolutionComponent>? containerSoln = null;
            Solution? containerSolution = null;

            if (chemMaster.Comp.SelectedBottleForFill >= 0 && chemMaster.Comp.StoredBottles[chemMaster.Comp.SelectedBottleForFill] is { } fillBottle && fromBuffer && isOutput)
            {
                container = fillBottle;
                if (!_solutionContainerSystem.TryGetSolution(container.Value, SharedChemMaster.BottleSolutionName, out containerSoln, out containerSolution))
                    return;
            }
            else if (chemMaster.Comp.SelectedBottleForFill >= 0 && chemMaster.Comp.StoredBottles[chemMaster.Comp.SelectedBottleForFill] is { } slotBottle)
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

            if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                return;

            if (fromBuffer) // Buffer to container
            {
                var solution = bufferSolution;
                // ADT-Tweak Start
                var available = solution.GetReagentQuantity(id);
                if (amount == int.MaxValue) amount = available; // Transfer all
                amount = FixedPoint2.Min(amount, available, containerSolution.AvailableVolume);
                // ADT-Tweak End
                amount = solution.RemoveReagent(id, amount, preserveOrder: true);
                _solutionContainerSystem.TryAddReagent(containerSoln.Value, id, amount, out _);
            }
            else // Container to buffer
            {
                // ADT-Tweak Start
                var available = containerSolution.GetReagentQuantity(id);
                if (amount == int.MaxValue) amount = available; // Transfer all
                amount = FixedPoint2.Min(amount, available);
                if (bufferSolution.MaxVolume.Value > 0)    //ADT-Tweak - chemicalbuffer if no limit
                    amount = FixedPoint2.Min(amount, available, bufferSolution.AvailableVolume);
                // ADT-Tweak End
                _solutionContainerSystem.RemoveReagent(containerSoln.Value, id, amount);

                var solution = bufferSolution;
                solution.AddReagent(id, amount);
            }

            UpdateUiState(chemMaster, updateLabel: true);
        }
        // ADT-Tweak-End

        private void DiscardReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer, bool isOutput)
        {
            if (fromBuffer)
            {
                if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out var bufferEntity, out var bufferSolution))
                    return;

                var solution = bufferSolution;
                // ADT-Tweak Start
                var available = solution.GetReagentQuantity(id);
                if (amount == int.MaxValue) amount = available; // Discard all
                amount = FixedPoint2.Min(amount, available);
                // ADT-Tweak End
                solution.RemoveReagent(id, amount, preserveOrder: true);
            }
            else
            {
                // ADT-Tweak Start
                EntityUid? container = null;
                Entity<SolutionComponent>? containerSoln = null;

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
                // ADT-Tweak End
                _solutionContainerSystem.RemoveReagent(containerSoln.Value, id, amount);
            }

            UpdateUiState(chemMaster, updateLabel: fromBuffer);
        }

        private void OnCreatePillsMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterCreatePillsMessage message)
        {
            var user = message.Actor;

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > chemMaster.Comp.PillDosageLimit)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            // Use reagent amounts if available, otherwise fall back to selected reagents
            if (chemMaster.Comp.SelectedReagentAmounts.Count > 0)
            {
                // Create pills using exact reagent amounts
                if (!WithdrawReagentAmountsFromBuffer(chemMaster, chemMaster.Comp.SelectedReagentAmounts, user, out var withdrawal))
                    return;

                // Calculate total volume for dosage calculation
                var totalVolume = FixedPoint2.Zero;
                foreach (var amount in chemMaster.Comp.SelectedReagentAmounts.Values)
                {
                    totalVolume += amount;
                }

                // Calculate how many pills we can create based on available volume and dosage
                var possiblePills = (int)(totalVolume / message.Dosage);
                var pillsToCreate = Math.Min(possiblePills, (int)message.Number);

                if (pillsToCreate == 0)
                    return;

                // Find available slots for storage
                var (totalAvailableSlots, availableSlots) = FindAvailablePillSlotsForCreation(chemMaster, pillsToCreate);

                // Calculate how many pills can be stored in canisters vs. dropped on ground
                var pillsToStore = Math.Min(pillsToCreate, totalAvailableSlots);
                var pillsToDrop = pillsToCreate - pillsToStore;

                // Create pills that can fit in canisters
                var pillIndex = 0;

                foreach (var (container, startSlot) in availableSlots)
                {
                    if (!TryComp(container, out StorageComponent? targetStorage))
                        continue;

                    var currentPillCount = targetStorage.Container.ContainedEntities.Count;
                    var maxPills = 10; // Standard pill canister capacity
                    var slotsAvailableInContainer = maxPills - currentPillCount;
                    var slotsToFillInContainer = Math.Min(slotsAvailableInContainer, pillsToStore - pillIndex);

                    for (int slotOffset = 0; slotOffset < slotsToFillInContainer && pillIndex < pillsToStore; slotOffset++)
                    {
                        var item = Spawn(PillPrototypeId, Transform(container).Coordinates);

                        var hasItemSolution = _solutionContainerSystem.EnsureSolutionEntity(
                            (item, null),
                            SharedChemMaster.PillSolutionName,
                            out var itemSolution,
                            message.Dosage);

                        if (!hasItemSolution || itemSolution is null)
                            continue;

                        // Split the withdrawal solution for this pill
                        var pillSolution = withdrawal.SplitSolution(message.Dosage);
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

                // Create pills that cannot fit in canisters - drop them on the ground
                for (int i = 0; i < pillsToDrop; i++)
                {
                    var item = Spawn(PillPrototypeId, Transform(chemMaster.Owner).Coordinates);

                    var hasItemSolution = _solutionContainerSystem.EnsureSolutionEntity(
                        (item, null),
                        SharedChemMaster.PillSolutionName,
                        out var itemSolution,
                        message.Dosage);

                    if (hasItemSolution && itemSolution is not null)
                    {
                        // Split the withdrawal solution for this pill
                        var pillSolution = withdrawal.SplitSolution(message.Dosage);
                        _solutionContainerSystem.TryAddSolution(itemSolution.Value, pillSolution);

                        var pill = EnsureComp<PillComponent>(item);
                        pill.PillType = chemMaster.Comp.PillType;
                        Dirty(item, pill);

                        _labelSystem.Label(item, message.Label);

                        // Log pill creation by a user
                        _adminLogger.Add(
                            LogType.Action,
                            LogImpact.Low,
                            $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Value.Comp.Solution)}");
                    }
                }

                // Show message if some pills could not be stored in canisters
                if (pillsToDrop > 0)
                {
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-pills-dropped-text",
                        ("dropped", pillsToDrop)), user);
                }
            }
            else
            {
                // Fall back to original logic if no reagent amounts are selected
                // Calculate total reagent volume needed for all requested pills
                var totalNeeded = message.Dosage * message.Number;

                if (!WithdrawSelectedReagentsFromBuffer(chemMaster, totalNeeded, user, out var withdrawal))
                    return;

                // Find available slots for storage (only used for canisters)
                var (totalAvailableSlots, availableSlots) = FindAvailablePillSlotsForCreation(chemMaster, (int)message.Number);

                // Calculate how many pills can be stored in canisters vs. dropped on ground
                var pillsToStore = Math.Min((int)message.Number, totalAvailableSlots);
                var pillsToDrop = (int)message.Number - pillsToStore;

                // Create pills that can fit in canisters
                var pillIndex = 0;

                foreach (var (container, startSlot) in availableSlots)
                {
                    if (!TryComp(container, out StorageComponent? targetStorage))
                        continue;

                    var currentPillCount = targetStorage.Container.ContainedEntities.Count;
                    var maxPills = 10; // Standard pill canister capacity
                    var slotsAvailableInContainer = maxPills - currentPillCount;
                    var slotsToFillInContainer = Math.Min(slotsAvailableInContainer, pillsToStore - pillIndex);

                    for (int slotOffset = 0; slotOffset < slotsToFillInContainer && pillIndex < pillsToStore; slotOffset++)
                    {
                        var item = Spawn(PillPrototypeId, Transform(container).Coordinates);

                        var hasItemSolution = _solutionContainerSystem.EnsureSolutionEntity(
                            (item, null),
                            SharedChemMaster.PillSolutionName,
                            out var itemSolution,
                            message.Dosage);

                        if (!hasItemSolution || itemSolution is null)
                            continue;

                        _solutionContainerSystem.TryAddSolution(itemSolution.Value, withdrawal.SplitSolution(message.Dosage));

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

                // Create pills that cannot fit in canisters - drop them on the ground
                for (int i = 0; i < pillsToDrop; i++)
                {
                    var item = Spawn(PillPrototypeId, Transform(chemMaster.Owner).Coordinates);

                    var hasItemSolution = _solutionContainerSystem.EnsureSolutionEntity(
                        (item, null),
                        SharedChemMaster.PillSolutionName,
                        out var itemSolution,
                        message.Dosage);

                    if (hasItemSolution && itemSolution is not null)
                    {
                        _solutionContainerSystem.TryAddSolution(itemSolution.Value, withdrawal.SplitSolution(message.Dosage));

                        var pill = EnsureComp<PillComponent>(item);
                        pill.PillType = chemMaster.Comp.PillType;
                        Dirty(item, pill);

                        _labelSystem.Label(item, message.Label);

                        // Log pill creation by a user
                        _adminLogger.Add(
                            LogType.Action,
                            LogImpact.Low,
                            $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Value.Comp.Solution)}");
                    }
                }

                // Show message if some pills could not be stored in canisters
                if (pillsToDrop > 0)
                {
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-pills-dropped-text",
                        ("dropped", pillsToDrop)), user);
                }
            }

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

            // Use reagent amounts if available, otherwise fall back to selected reagents
            if (chemMaster.Comp.SelectedReagentAmounts.Count > 0)
            {
                // Create bottles using exact reagent amounts
                if (!WithdrawReagentAmountsFromBuffer(chemMaster, chemMaster.Comp.SelectedReagentAmounts, user, out var withdrawal))
                    return;

                // Calculate total volume for dosage calculation
                var totalVolume = FixedPoint2.Zero;
                foreach (var amount in chemMaster.Comp.SelectedReagentAmounts.Values)
                {
                    totalVolume += amount;
                }

                // Calculate how many bottles we can create based on available volume and dosage
                var possibleBottles = (int)(totalVolume / message.Dosage);
                var bottlesToCreate = Math.Min(possibleBottles, (int)message.Number);

                if (bottlesToCreate == 0)
                    return;

                // ADT-Tweak Start: Bottle buttons reagent transfer
                // Build a list of eligible target bottles in slot order.
                // Preference: EMPTY bottles only (do not touch previously-filled bottles).
                var targets = new List<EntityUid>(capacity: bottlesToCreate);
                for (int i = 0; i < chemMaster.Comp.StoredBottles.Count && targets.Count < bottlesToCreate; i++)
                {
                    var ent = chemMaster.Comp.StoredBottles[i];
                    if (!ent.HasValue)
                        continue;

                    if (!_solutionContainerSystem.TryGetSolution(ent.Value, SharedChemMaster.BottleSolutionName, out var soln, out var solution))
                        continue;

                    // Only select empty bottles; also ensure there is enough free volume for the dosage.
                    if (solution.Volume == 0 && solution.AvailableVolume >= message.Dosage)
                        targets.Add(ent.Value);
                }

                // If there are fewer empty bottles than requested, only fill what we can.
                if (targets.Count == 0)
                    return;

                var actualCount = Math.Min(bottlesToCreate, targets.Count);

                for (int i = 0; i < actualCount; i++)
                {
                    var bottle = targets[i];
                    if (!_solutionContainerSystem.TryGetSolution(bottle, SharedChemMaster.BottleSolutionName, out var soln, out var solution))
                        continue;

                    if (message.Dosage > solution.AvailableVolume)
                        continue;

                    _labelSystem.Label(bottle, message.Label);

                    // Split the withdrawal solution for this bottle
                    var bottleSolution = withdrawal.SplitSolution(message.Dosage);
                    _solutionContainerSystem.TryAddSolution(soln.Value, bottleSolution);

                    // Log bottle fill by a user
                    _adminLogger.Add(LogType.Action, LogImpact.Low,
                        $"{ToPrettyString(user):user} bottled {ToPrettyString(bottle):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
                }
            }
            else
            {
                // Fall back to original logic if no reagent amounts are selected
                // ADT-Tweak Start: Bottle buttons reagent transfer
                // Build a list of eligible target bottles in slot order.
                // Preference: EMPTY bottles only (do not touch previously-filled bottles).
                var targets = new List<EntityUid>(capacity: (int) message.Number);
                for (int i = 0; i < chemMaster.Comp.StoredBottles.Count && targets.Count < message.Number; i++)
                {
                    var ent = chemMaster.Comp.StoredBottles[i];
                    if (!ent.HasValue)
                        continue;

                    if (!_solutionContainerSystem.TryGetSolution(ent.Value, SharedChemMaster.BottleSolutionName, out var soln, out var solution))
                        continue;

                    // Only select empty bottles; also ensure there is enough free volume for the dosage.
                    if (solution.Volume == 0 && solution.AvailableVolume >= message.Dosage)
                        targets.Add(ent.Value);
                }

                // If there are fewer empty bottles than requested, only fill what we can.
                if (targets.Count == 0)
                    return;

                var actualCount = (uint) Math.Min((int) message.Number, targets.Count);
                var needed = message.Dosage * actualCount;

                if (!WithdrawSelectedReagentsFromBuffer(chemMaster, needed, user, out var withdrawal))
                    return;

                for (int i = 0; i < actualCount; i++)
                {
                    var bottle = targets[i];
                    if (!_solutionContainerSystem.TryGetSolution(bottle, SharedChemMaster.BottleSolutionName, out var soln, out var solution))
                        continue;

                    if (message.Dosage > solution.AvailableVolume)
                        continue;

                    _labelSystem.Label(bottle, message.Label);
                    _solutionContainerSystem.TryAddSolution(soln.Value, withdrawal.SplitSolution(message.Dosage));

                    // Log bottle fill by a user
                    _adminLogger.Add(LogType.Action, LogImpact.Low,
                        $"{ToPrettyString(user):user} bottled {ToPrettyString(bottle):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
                }
            }

            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }
        // ADT-Tweak End

        private bool WithdrawFromBuffer(
            Entity<ChemMasterComponent> chemMaster,
            FixedPoint2 neededVolume, EntityUid? user,
            [NotNullWhen(returnValue: true)] out Solution? outputSolution)
        {
            outputSolution = null;

            if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var solution))
                return false;

            if (solution.Volume == 0)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), user.Value);
                return false;
            }

            // ReSharper disable once InvertIf
            if (neededVolume > solution.Volume)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), user.Value);
                return false;
            }

            outputSolution = solution.SplitSolution(neededVolume);
            return true;
        }

        private bool WithdrawSelectedReagentsFromBuffer(
            Entity<ChemMasterComponent> chemMaster,
            FixedPoint2 neededVolume, EntityUid? user,
            [NotNullWhen(returnValue: true)] out Solution? outputSolution)
        {
            outputSolution = null;

            if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var solution))
                return false;

            if (solution.Volume == 0)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), user.Value);
                return false;
            }

            // Get selected reagents for creation
            var selectedReagents = chemMaster.Comp.SelectedReagentsForBottles;

            // Check if any reagents are selected
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
        }

        private bool WithdrawReagentAmountsFromBuffer(
            Entity<ChemMasterComponent> chemMaster,
            Dictionary<ReagentId, int> reagentAmounts, EntityUid? user,
            [NotNullWhen(returnValue: true)] out Solution? outputSolution)
        {
            outputSolution = null;

            if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var solution))
                return false;

            if (solution.Volume == 0)
            {
                if (user.HasValue)
                    _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), user.Value);
                return false;
            }

            // Check if all requested reagents are available in sufficient quantities
            var totalNeededVolume = FixedPoint2.Zero;
            foreach (var (reagent, amount) in reagentAmounts)
            {
                var available = solution.GetReagentQuantity(reagent);
                if (available < amount)
                {
                    if (user.HasValue)
                        _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), user.Value);
                    return false;
                }
                totalNeededVolume += amount;
            }

            // Create a new solution with the exact reagent amounts
            outputSolution = new Solution();

            foreach (var (reagent, amount) in reagentAmounts)
            {
                var actualAmount = solution.RemoveReagent(reagent, FixedPoint2.New(amount), preserveOrder: true);
                outputSolution.AddReagent(reagent, actualAmount);
            }

            return true;
        }

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

        // ADT-Tweak-Start
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

        // ADT-Tweak Start: Bottle buttons reagent transfer
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

        private void OnReagentToggledOnMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterReagentToggledOnMessage message)
        {
            if (!chemMaster.Comp.SelectedReagentsForBottles.Contains(message.Reagent))
            {
                chemMaster.Comp.SelectedReagentsForBottles.Add(message.Reagent);
            }
            UpdateUiState(chemMaster);
        }

        private void OnReagentToggledOffMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterReagentToggledOffMessage message)
        {
            chemMaster.Comp.SelectedReagentsForBottles.Remove(message.Reagent);
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
                if (_itemSlotsSystem.TryGetSlot(chemMaster.Owner, slotName, out var itemSlot))
                    _itemSlotsSystem.TryEject(chemMaster.Owner, itemSlot, message.Actor, out _, excludeUserAudio: true);
            }
            UpdateUiState(chemMaster);
        }

        private void OnMapInit(EntityUid uid, ChemMasterComponent component, MapInitEvent args)
        {
            // ADT-Tweak: Pill container slots (3 slots for pill containers)
            for (int i = 0; i < 3; i++)
            {
                var slotId = "pillContainerSlot" + i;
                ItemSlot slot = new();
                var whitelist = new EntityWhitelist();
                whitelist.Tags = new List<ProtoId<TagPrototype>> { "PillCanister" };
                slot.Whitelist = whitelist;
                _itemSlotsSystem.AddItemSlot(uid, slotId, slot);
            }

            // ADT-Tweak: Legacy bottle slots (20 slots for bottles)
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
                chemMaster.Comp.StoredPillContainers[containerIndex] is { } pillContainer &&
                pillContainer != null)
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
                    chemMaster.Comp.StoredPillContainers[containerIndex] is { } pillContainer &&
                    pillContainer != null)
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

            chemMaster.Comp.SelectedPillCanisterForCreation = message.CanisterIndex;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnItemSlotButtonPressed(Entity<ChemMasterComponent> chemMaster, ref ItemSlotButtonPressedEvent message)
        {
            if (message.SlotId.StartsWith("pillContainerSlot") && int.TryParse(message.SlotId.Replace("pillContainerSlot", ""), out int canisterIndex) && canisterIndex >= 0 && canisterIndex < 3)
            {
                var slotId = $"pillContainerSlot{canisterIndex}";
                if (_itemSlotsSystem.TryGetSlot(chemMaster.Owner, slotId, out var slot) && slot.Item.HasValue)
                {
                    _itemSlotsSystem.TryEject(chemMaster.Owner, slot, message.Actor, out _, excludeUserAudio: true);
                }

                UpdateUiState(chemMaster);
            }
        }

        // ADT-Tweak: Reagent amount selection handlers
        private void OnSelectReagentAmountMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSelectReagentAmountMessage message)
        {
            // Set the exact amount for this reagent (replace existing value)
            chemMaster.Comp.SelectedReagentAmounts[message.Reagent] = message.Amount;

            UpdateUiState(chemMaster, updateLabel: true);
            ClickSound(chemMaster);
        }

        private void OnRemoveReagentAmountMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterRemoveReagentAmountMessage message)
        {
            if (!chemMaster.Comp.SelectedReagentAmounts.ContainsKey(message.Reagent))
                return;

            chemMaster.Comp.SelectedReagentAmounts[message.Reagent] = Math.Max(0, chemMaster.Comp.SelectedReagentAmounts[message.Reagent] - message.Amount);
            if (chemMaster.Comp.SelectedReagentAmounts[message.Reagent] == 0)
            {
                chemMaster.Comp.SelectedReagentAmounts.Remove(message.Reagent);
            }
            UpdateUiState(chemMaster, updateLabel: true);
            ClickSound(chemMaster);
        }

        private void OnClearReagentAmountMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterClearReagentAmountMessage message)
        {
            chemMaster.Comp.SelectedReagentAmounts.Remove(message.Reagent);
            UpdateUiState(chemMaster, updateLabel: true);
            ClickSound(chemMaster);
        }

        // ADT-Tweak End
    }
}
