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

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChemMasterComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetModeMessage>(OnSetModeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetPillTypeMessage>(OnSetPillTypeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterReagentAmountButtonMessage>(OnReagentButtonMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterCreatePillsMessage>(OnCreatePillsMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterOutputToBottleMessage>(OnOutputToBottleMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSortMethodUpdated>(OnSortMethodUpdated);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterTransferringAmountUpdated>(OnTransferringAmountUpdated);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterAmountsUpdated>(OnAmountsUpdated);
        }

        private void OnAmountsUpdated(Entity<ChemMasterComponent> ent, ref ChemMasterAmountsUpdated args)
        {
            ent.Comp.Amounts = args.Amounts;    //ADT-Tweak
            UpdateUiState(ent);
        }

        //ADT-Tweak Start
        private void SubscribeUpdateUiState<T>(Entity<ChemMasterComponent> ent, ref T ev) =>
            UpdateUiState(ent);
        //ADT-Tweak End
        private void UpdateUiState(Entity<ChemMasterComponent> ent, bool updateLabel = false)
        {
            var (owner, chemMaster) = ent;

            if (!_solutionContainerSystem.TryGetSolution(owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                return;

            //ADT-Tweak Start
            if (!_solutionContainerSystem.TryGetSolution(owner, SharedChemMaster.PillBufferSolutionName, out _, out var pillBufferSolution))
                return;
            //ADT-Tweak End
            var container = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.InputSlotName);

            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.Volume;

            var pillBufferReagents = pillBufferSolution.Contents;
            var pillBufferCurrentVolume = pillBufferSolution.Volume;

            //ADT-Tweak Start
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
                chemMaster.Amounts);
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

        // ADT-Tweak-Start: Расширенная логика для работы с двумя буферами
        private void TransferReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer, bool isOutput)
        {
            var container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
            if (container is null ||
                !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerEntity, out var containerSolution) ||
                !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution) ||
                !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.PillBufferSolutionName, out _, out var pillBufferSolution))
                return;

            if (fromBuffer) // Buffer to container
            {
                amount = FixedPoint2.Min(amount, containerSolution.AvailableVolume);
                var solution = isOutput ? pillBufferSolution : bufferSolution;

                amount = solution.RemoveReagent(id, amount, preserveOrder: true);
                _solutionContainerSystem.TryAddReagent(containerEntity.Value, id, amount, out _);
            }
            else // Container to buffer
            {
                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id));
                if (bufferSolution.MaxVolume.Value > 0)    //ADT-Tweak - chemicalbuffer if no limit
                    amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id), bufferSolution.AvailableVolume);

                _solutionContainerSystem.RemoveReagent(containerEntity.Value, id, amount);

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

                amount = FixedPoint2.Min(amount, solution.GetReagentQuantity(id));
                solution.RemoveReagent(id, amount, preserveOrder: true);
            }
            else
            {
                var container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);

                if (container is null ||
                    !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerEntity, out var containerSolution))
                    return;

                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id));
                _solutionContainerSystem.RemoveReagent(containerEntity.Value, id, amount);
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
            var needed = message.Dosage * message.Number;
            var maybeContainer = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.OutputSlotName);
            Entity<SolutionComponent>? soln;
            Solution? solution;

            if (maybeContainer == null)
            {
                return;
            }

            if (maybeContainer is not { Valid: true } container)
                return; // output can't fit reagents

            // Ensure the amount is valid.
            if (message.Dosage == 0)
                return;

            // Ensure the amount is valid.
            if (message.Dosage == 0 || message.Dosage > chemMaster.Comp.BottleDosageLimit)
                return;

            // Ensure label length is within the character limit.
            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            if (_solutionContainerSystem.TryGetSolution(container,
                    SharedChemMaster.BottleSolutionName,
                    out soln,
                    out solution) && message.Number == 1)
            {
                if (message.Dosage > solution.AvailableVolume)
                    return;
                if (!WithdrawFromBuffer(chemMaster, message.Dosage, user, out var withdrawal))
                    return;

                _labelSystem.Label(container, message.Label);
                _solutionContainerSystem.TryAddSolution(soln.Value, withdrawal);

                // Log bottle fill by a user
                _adminLogger.Add(LogType.Action, LogImpact.Low,
                    $"{ToPrettyString(user):user} bottled {ToPrettyString(container):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution)}");
                UpdateUiState(chemMaster);
                ClickSound(chemMaster);
                return;
            }

            if (TryComp<StorageComponent>(container, out var storage))
            {
                List<EntityUid> bottles = new List<EntityUid>();
                foreach (var ent in storage.Container.ContainedEntities)
                {
                    if (!_solutionContainerSystem.TryGetSolution(ent,
                            SharedChemMaster.BottleSolutionName,
                            out soln,
                            out solution) || message.Dosage > solution.AvailableVolume)
                        continue;
                    bottles.Add(ent);
                }
                if (bottles.Count < message.Number)
                    return; // Check for enough bottles

                if (!WithdrawFromBuffer(chemMaster, needed, user, out var withdrawal))
                    return;

                _labelSystem.Label(container, message.Label);
                foreach (var bottle in bottles)
                {
                    _solutionContainerSystem.TryGetSolution(bottle,
                        SharedChemMaster.BottleSolutionName,
                        out soln,
                        out solution);

                    _labelSystem.Label(bottle, message.Label);
                    _solutionContainerSystem.TryAddSolution(soln!.Value, withdrawal.SplitSolution(message.Dosage));

                    // Log bottle fill by a user
                    _adminLogger.Add(LogType.Action, LogImpact.Low,
                        $"{ToPrettyString(user):user} bottled {ToPrettyString(container):bottle} {SharedSolutionContainerSystem.ToPrettyString(solution!)}");
                }
                UpdateUiState(chemMaster);
                ClickSound(chemMaster);
            }
        }

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
        // ADT-Tweak-End
    }
}
