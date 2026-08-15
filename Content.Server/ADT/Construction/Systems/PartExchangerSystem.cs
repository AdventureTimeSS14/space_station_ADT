using System.Linq;
using Content.Server.ADT.Construction.Components;
using Content.Server.Beam;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Stack;
using Content.Server.Storage.EntitySystems;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.ADT.Construction.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Exchanger;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Construction.Systems;

/// <summary>
/// Handles the Rapid Part Exchanger: swaps machine parts in assembled machines
/// and can finish a machine frame from parts stored inside.
/// </summary>
public sealed class PartExchangerSystem : EntitySystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StorageSystem _storage = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly MachineFrameSystem _machineFrame = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly BeamSystem _beam = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MachineComponent, InteractUsingEvent>(OnMachineInteractUsing);
        SubscribeLocalEvent<PartExchangerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PartExchangerComponent, ExchangerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PartExchangerComponent, PartExchangerFilterActionEvent>(OnFilterAction);
        SubscribeLocalEvent<PartExchangerComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<PartExchangerComponent, PartExchangerFilterChangedMessage>(OnFilterChanged);
    }

    private void OnFilterAction(EntityUid uid, PartExchangerComponent component, PartExchangerFilterActionEvent args)
    {
        _ui.OpenUi(uid, PartExchangerUiKey.Key, args.Performer);
        args.Handled = true;
    }

    private void OnUiOpened(EntityUid uid, PartExchangerComponent component, BoundUIOpenedEvent args)
    {
        if (args.UiKey is not PartExchangerUiKey uiKey || uiKey != PartExchangerUiKey.Key)
            return;

        UpdateUiState(uid, component);
    }

    private void OnFilterChanged(EntityUid uid, PartExchangerComponent component, PartExchangerFilterChangedMessage args)
    {
        component.FilteredParts = args.SelectedParts;
        UpdateUiState(uid, component);

        var message = component.FilteredParts.Count == 0
            ? Loc.GetString("part-exchanger-filter-reset")
            : Loc.GetString("part-exchanger-filter-set");
        _popup.PopupEntity(message, uid, args.Actor);
    }

    private void UpdateUiState(EntityUid uid, PartExchangerComponent component)
    {
        var available = new HashSet<ProtoId<MachinePartPrototype>>();
        if (TryComp<StorageComponent>(uid, out var storage))
        {
            foreach (var itemUid in storage.Container.ContainedEntities)
            {
                if (!TryComp<MachinePartComponent>(itemUid, out var part))
                    continue;

                available.Add(part.Part);
            }
        }

        var ordered = available
            .OrderBy(id => _proto.TryIndex(id, out var partProto) ? partProto.SortPriority : int.MaxValue)
            .ToList();

        _ui.SetUiState(uid, PartExchangerUiKey.Key, new PartExchangerFilterState
        {
            AvailableParts = ordered,
            SelectedParts = component.FilteredParts,
        });
    }

    private void OnMachineInteractUsing(EntityUid uid, MachineComponent component, InteractUsingEvent args)
    {
        TryStartExchange(uid, args);
    }

    private bool CanStartExchange(EntityUid user, EntityUid target, PartExchangerComponent exchanger)
    {
        if (exchanger.DoDistanceCheck && !_interactionSystem.InRangeUnobstructed(user, target))
            return false;

        if (!exchanger.RequireOpenPanel || !TryComp<WiresPanelComponent>(target, out var panel) || panel.Open)
            return true;

        _popup.PopupEntity(Loc.GetString("construction-step-condition-wire-panel-open"), target, user);
        return false;
    }

    private bool TryStartExchangeDoAfter(EntityUid user, EntityUid used, EntityUid target, PartExchangerComponent exchanger)
    {
        if (exchanger.ExchangeBeamPrototype is { } beamPrototype)
            _beam.TryCreateBeam(user, target, beamPrototype);

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, exchanger.ExchangeDuration, new ExchangerDoAfterEvent(), used, target: target, used: used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DistanceThreshold = exchanger.DoDistanceCheck
                ? 1.2f
                : null,
            RequireCanInteract = exchanger.DoDistanceCheck,
        });
    }

    private void OnAfterInteract(EntityUid uid, PartExchangerComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.CanReach || component.DoDistanceCheck)
            return;

        if (args.Target is not { } target)
            return;

        if (!HasComp<MachineComponent>(target) && !HasComp<MachineFrameComponent>(target))
            return;

        args.Handled = true;

        if (!CanStartExchange(args.User, target, component))
            return;

        if (TryStartExchangeDoAfter(args.User, uid, target, component))
            _audio.PlayPvs(component.ExchangeSound, uid);
    }

    public bool TryStartExchange(EntityUid target, InteractUsingEvent args)
    {
        if (args.Handled)
            return false;

        if (!TryComp<PartExchangerComponent>(args.Used, out var exchanger))
            return false;

        args.Handled = true;

        if (!CanStartExchange(args.User, target, exchanger))
            return true;

        TryStartExchangeDoAfter(args.User, args.Used, target, exchanger);
        return true;
    }

    private void OnDoAfter(EntityUid uid, PartExchangerComponent component, DoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Args.Target is not { } target)
            return;

        if (!TryComp<StorageComponent>(uid, out var storage))
            return;

        if (TryComp<MachineFrameComponent>(target, out var machineFrame))
        {
            args.Handled = TryInsertIntoMachineFrame(uid, target, storage, machineFrame);

            if (args.Handled)
                _audio.PlayPvs(component.ExchangeSound, uid);

            return;
        }

        if (!TryComp<MachineComponent>(target, out var machine))
            return;

        if (!TryComp<MachinePartStorageComponent>(target, out var machineStorage))
            return;

        var storageParts = new Dictionary<ProtoId<MachinePartPrototype>, List<(EntityUid Uid, MachinePartState State)>>();

        foreach (var partUid in storage.Container.ContainedEntities)
        {
            if (!_construction.GetMachinePartState(partUid, out var partState))
                continue;

            if (!storageParts.TryGetValue(partState.Part.Part, out var bucket))
            {
                bucket = new();
                storageParts[partState.Part.Part] = bucket;
            }
            bucket.Add((partUid, partState));
        }

        var changed = false;

        foreach (var partType in machineStorage.Parts.Select(slot => slot.Part).Distinct().ToArray())
        {
            if (component.FilteredParts.Count > 0 && !component.FilteredParts.Contains(partType))
                continue;

            if (!storageParts.TryGetValue(partType, out var available))
                continue;

            available.Sort((a, b) => b.State.Part.Tier.CompareTo(a.State.Part.Tier));

            foreach (var currentSlot in machineStorage.Parts.Where(slot => slot.Part == partType).OrderBy(slot => slot.Tier).ToArray())
            {
                var needed = currentSlot.Quantity;
                var collected = new List<(EntityUid Uid, int Amount, MachinePartState State)>();
                var collectedTotal = 0;

                for (var i = 0; i < available.Count && collectedTotal < needed; i++)
                {
                    var candidate = available[i];
                    if (candidate.State.Part.Tier <= currentSlot.Tier)
                        continue;

                    var candidateQuantity = candidate.State.Quantity();
                    var used = Math.Min(needed - collectedTotal, candidateQuantity);
                    if (used <= 0)
                        continue;

                    collected.Add((candidate.Uid, used, candidate.State));
                    collectedTotal += used;
                }

                if (collectedTotal < needed)
                    continue;

                if (!_proto.TryIndex(partType, out var partProto))
                    continue;

                var replacementUids = new List<EntityUid>();
                var reservationFailed = false;

                void RollbackReserved()
                {
                    foreach (var reservedUid in replacementUids)
                    {
                        if (TerminatingOrDeleted(reservedUid))
                            continue;

                        _container.Insert(reservedUid, storage.Container, force: true);
                    }
                }

                foreach (var (replacementUid, amount, state) in collected)
                {
                    if (!_container.TryRemoveFromContainer(replacementUid, force: true))
                    {
                        reservationFailed = true;
                        break;
                    }

                    if (state.Stack != null && state.Stack.Count > amount)
                    {
                        var split = _stack.Split((replacementUid, state.Stack), amount, Transform(target).Coordinates);
                        if (split == null)
                        {
                            _container.Insert(replacementUid, storage.Container, force: true);
                            reservationFailed = true;
                            break;
                        }

                        replacementUids.Add(split.Value);
                        available.Add((split.Value, state));
                        _container.Insert(replacementUid, storage.Container, force: true);
                    }
                    else
                    {
                        replacementUids.Add(replacementUid);
                    }
                }

                if (reservationFailed)
                {
                    RollbackReserved();
                    continue;
                }

                var oldPrototype = partProto.GetTierPrototype(currentSlot.Tier);
                var spawnedOldParts = new List<EntityUid>();
                for (var i = 0; i < needed; i++)
                {
                    var spawned = Spawn(oldPrototype, Transform(target).Coordinates);
                    spawnedOldParts.Add(spawned);

                    if (!_storage.Insert(uid, spawned, out _, playSound: false))
                    {
                        reservationFailed = true;
                        break;
                    }
                }

                if (reservationFailed)
                {
                    foreach (var spawned in spawnedOldParts)
                        QueueDel(spawned);

                    RollbackReserved();
                    continue;
                }

                foreach (var (replacementUid, amount, state) in collected)
                {
                    machineStorage.Parts.Add(new MachinePartSlot { Part = partType, Tier = state.Part.Tier, Quantity = amount });
                    QueueDel(replacementUid);
                }

                machineStorage.Parts.Remove(currentSlot);

                foreach (var removedUid in replacementUids)
                {
                    var index = available.FindIndex(p => p.Uid == removedUid);
                    if (index < 0)
                        continue;

                    var stack = available[index].State.Stack;
                    if (stack == null || stack.Count == 0)
                        available.RemoveAt(index);
                }

                changed = true;
            }
        }

        if (changed)
        {
            _construction.RefreshParts(target, machine);
            _audio.PlayPvs(component.ExchangeSound, uid);
        }

        args.Handled = true;
    }

    private bool TryInsertIntoMachineFrame(EntityUid user, EntityUid frameUid, StorageComponent storage, MachineFrameComponent machineFrame)
    {
        var changed = false;

        if (!machineFrame.HasBoard)
        {
            foreach (var itemUid in storage.Container.ContainedEntities.ToArray())
            {
                if (!_machineFrame.TryInsertBoard(frameUid, itemUid, machineFrame))
                    continue;

                changed = true;
                break;
            }

            if (!machineFrame.HasBoard)
                return changed;
        }

        foreach (var itemUid in storage.Container.ContainedEntities.ToArray())
        {
            if (TryComp<MachinePartComponent>(itemUid, out var machinePart)
                && _machineFrame.TryInsertMachinePart(frameUid, itemUid, machineFrame, machinePart))
            {
                changed = true;
                continue;
            }

            if (TryComp<StackComponent>(itemUid, out var stack)
                && _machineFrame.TryInsertStack(frameUid, itemUid, machineFrame, stack))
            {
                changed = true;
                continue;
            }

            if (_machineFrame.TryInsertRequirements(frameUid, itemUid, machineFrame))
                changed = true;
        }

        if (!changed)
            return false;

        _machineFrame.RegenerateProgress(machineFrame);

        if (_machineFrame.IsComplete(machineFrame))
            _popup.PopupEntity(Loc.GetString("machine-frame-component-on-complete"), frameUid, user);

        return true;
    }
}
