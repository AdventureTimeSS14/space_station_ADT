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

        [ValidatePrototypeId<EntityPrototype>]
        private const string PillPrototypeId = "Pill";

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
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterToggleBottleFillMessage>(OnToggleBottleFillMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterRowEjectMessage>(OnRowEjectMessage);
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

            //ADT-Tweak Start
            if (!_solutionContainerSystem.TryGetSolution(owner, SharedChemMaster.PillBufferSolutionName, out _, out var pillBufferSolution))
                return;

            // Initialize stored bottles list with correct size if needed
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

            var pillBufferReagents = pillBufferSolution.Contents;
            var pillBufferCurrentVolume = pillBufferSolution.Volume;

            // ADT-Tweak
            var storedBottlesInfo = new List<ContainerInfo?>();
            // Clear the stored bottles list to rebuild it in correct order
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
                pillBufferReagents,
                bufferCurrentVolume,
                pillBufferCurrentVolume,
                chemMaster.PillType,
                chemMaster.PillDosageLimit,
                chemMaster.BottleDosageLimit,
                updateLabel,
                chemMaster.SortMethod,
                chemMaster.TransferringAmount,
                chemMaster.Amounts,
                storedBottlesInfo,
                chemMaster.SelectedBottleSlot,
                chemMaster.SelectedBottleForFill,
                chemMaster.SelectedReagentsForBottles,
                chemMaster.SelectedReagent);
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
            else if (chemMaster.Comp.SelectedBottleSlot >= 0 && chemMaster.Comp.StoredBottles[chemMaster.Comp.SelectedBottleSlot] is { } slotBottle)
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

            if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution) ||
                !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.PillBufferSolutionName, out _, out var pillBufferSolution))
                return;

            if (fromBuffer) // Buffer to container
            {
                var solution = isOutput ? pillBufferSolution : bufferSolution;
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

                var solution = isOutput ? pillBufferSolution : bufferSolution;
                solution.AddReagent(id, amount);
            }

            UpdateUiState(chemMaster, updateLabel: true);
        }
        // ADT-Tweak-End

        private void DiscardReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer, bool isOutput)
        {
            if (fromBuffer)
            {
                if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out var bufferEntity, out var bufferSolution) ||
                    !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.PillBufferSolutionName, out var pillBufferEntity, out var pillBufferSolution))
                    return;

                var solution = isOutput ? pillBufferSolution : bufferSolution;
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

                if (chemMaster.Comp.SelectedBottleSlot >= 0 && chemMaster.Comp.StoredBottles[chemMaster.Comp.SelectedBottleSlot] is { } bottle)
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
            var maybeContainer = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.OutputSlotName);

            if (maybeContainer == null)
            {
                return;
            }

            if (maybeContainer is not { Valid: true } container
                || !TryComp(container, out StorageComponent? storage))
            {
                return; // output can't fit pills
            }

            // Ensure the number is valid.
            if (message.Number == 0 || !_storageSystem.HasSpace((container, storage)))
                return;

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > chemMaster.Comp.PillDosageLimit)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            var needed = message.Dosage * message.Number;
            if (!WithdrawFromBuffer(chemMaster, needed, user, out var withdrawal))
                return;

            _labelSystem.Label(container, message.Label);

            for (var i = 0; i < message.Number; i++)
            {
                var item = Spawn(PillPrototypeId, Transform(container).Coordinates);
                _storageSystem.Insert(container, item, out _, user: user, storage);
                _labelSystem.Label(item, message.Label);

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

                // Log pill creation by a user
                _adminLogger.Add(
                    LogType.Action,
                    LogImpact.Low,
                    $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Value.Comp.Solution)}");
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

            if (!WithdrawFromBuffer(chemMaster, needed, user, out var withdrawal))
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

            if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.PillBufferSolutionName, out _, out var solution))
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
                    _itemSlotsSystem.TryEject(chemMaster.Owner, itemSlot, chemMaster.Owner, out _, excludeUserAudio: true);
            }
            UpdateUiState(chemMaster);
        }

        private void OnMapInit(EntityUid uid, ChemMasterComponent component, MapInitEvent args)
        {
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
        // ADT-Tweak-End
    }
}
