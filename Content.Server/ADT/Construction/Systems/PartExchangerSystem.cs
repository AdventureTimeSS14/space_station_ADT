// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using System.Linq;
using Content.Server.ADT.Construction.Components;
using Content.Server.Beam;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Stack;
using Content.Server.Storage.EntitySystems;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.ADT.Construction.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Exchanger;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Content.Shared.Wires;
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
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly BeamSystem _beam = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MachineComponent, InteractUsingEvent>(OnMachineInteractUsing);
        SubscribeLocalEvent<PartExchangerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<PartExchangerComponent, ExchangerDoAfterEvent>(OnDoAfter);
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

        var machineParts = new Dictionary<ProtoId<MachinePartPrototype>, List<(EntityUid Uid, MachinePartState State)>>();
        var storageParts = new Dictionary<ProtoId<MachinePartPrototype>, List<(EntityUid Uid, MachinePartState State)>>();

        foreach (var partUid in machine.PartContainer.ContainedEntities)
        {
            if (!_construction.GetMachinePartState(partUid, out var partState))
                continue;

            if (!machineParts.TryGetValue(partState.Part.Part, out var bucket))
            {
                bucket = new();
                machineParts[partState.Part.Part] = bucket;
            }
            bucket.Add((partUid, partState));
        }

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

        foreach (var (partType, current) in machineParts)
        {
            if (!storageParts.TryGetValue(partType, out var available))
                continue;

            available.Sort((a, b) => b.State.Part.Tier.CompareTo(a.State.Part.Tier));

            foreach (var currentPart in current.OrderBy(part => part.State.Part.Tier))
            {
                var needed = currentPart.State.Quantity();
                var collected = new List<(EntityUid Uid, int Amount, MachinePartState State)>();
                var collectedTotal = 0;

                for (var i = 0; i < available.Count && collectedTotal < needed; i++)
                {
                    var candidate = available[i];
                    if (candidate.State.Part.Tier <= currentPart.State.Part.Tier)
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

                var replacementUids = new List<EntityUid>();
                var removed = new List<(EntityUid Uid, int Amount)>();
                var reservationFailed = false;
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

                    removed.Add((Uid: replacementUid, Amount: amount));
                }

                if (reservationFailed)
                {
                    foreach (var reservedUid in replacementUids)
                    {
                        if (TerminatingOrDeleted(reservedUid))
                            continue;

                        _container.Insert(reservedUid, storage.Container, force: true);
                    }

                    continue;
                }

                _container.RemoveEntity(target, currentPart.Uid);

                if (!_storage.Insert(uid, currentPart.Uid, out _, playSound: false))
                {
                    _container.Insert(currentPart.Uid, machine.PartContainer, force: true);
                    foreach (var reservedUid in replacementUids)
                    {
                        if (TerminatingOrDeleted(reservedUid))
                            continue;

                        _container.Insert(reservedUid, storage.Container, force: true);
                    }

                    continue;
                }

                foreach (var replacementUid in replacementUids)
                {
                    if (!_container.Insert(replacementUid, machine.PartContainer, force: true))
                        _container.Insert(replacementUid, storage.Container, force: true);
                }

                foreach (var (removedUid, _) in removed)
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

        var remainingParts = machineFrame.PartRequirements.ToDictionary(
            entry => entry.Key,
            entry => Math.Max(0, entry.Value - machineFrame.PartProgress.GetValueOrDefault(entry.Key)));
        var remainingMaterials = machineFrame.MaterialRequirements.ToDictionary(
            entry => entry.Key,
            entry => Math.Max(0, entry.Value - machineFrame.MaterialProgress.GetValueOrDefault(entry.Key)));
        var remainingComponents = machineFrame.ComponentRequirements.ToDictionary(
            entry => entry.Key,
            entry => Math.Max(0, entry.Value.Amount - machineFrame.ComponentProgress.GetValueOrDefault(entry.Key)));
        var remainingTags = machineFrame.TagRequirements.ToDictionary(
            entry => entry.Key,
            entry => Math.Max(0, entry.Value.Amount - machineFrame.TagProgress.GetValueOrDefault(entry.Key)));

        foreach (var partUid in storage.Container.ContainedEntities.ToArray())
        {
            if (TryComp<MachinePartComponent>(partUid, out var machinePart)
                && remainingParts.TryGetValue(machinePart.Part, out var partRemaining)
                && partRemaining > 0)
            {
                var count = TryComp<StackComponent>(partUid, out var partStack) ? partStack.Count : 1;
                var amount = Math.Min(partRemaining, count);
                var partToInsert = partUid;

                if (amount <= 0)
                    continue;

                if (partStack != null && partStack.Count > amount)
                {
                    var split = _stack.Split((partUid, partStack), amount, Transform(frameUid).Coordinates);
                    if (split == null)
                        continue;

                    partToInsert = split.Value;
                }
                else if (!_container.TryRemoveFromContainer(partUid, force: true))
                {
                    continue;
                }

                if (!_container.Insert(partToInsert, machineFrame.PartContainer))
                {
                    _container.Insert(partToInsert, storage.Container, force: true);
                    continue;
                }

                remainingParts[machinePart.Part] = Math.Max(0, partRemaining - amount);
                changed = true;
                continue;
            }

            if (TryComp<StackComponent>(partUid, out var stack)
                && remainingMaterials.TryGetValue(stack.StackTypeId, out var materialRemaining)
                && materialRemaining > 0)
            {
                var materialAmount = Math.Min(materialRemaining, stack.Count);
                EntityUid? stackToInsert = partUid;

                if (materialAmount > 0)
                {
                    if (stack.Count > materialAmount)
                    {
                        var split = _stack.Split((partUid, stack), materialAmount, Transform(frameUid).Coordinates);
                        if (split != null)
                            stackToInsert = split.Value;
                    }
                    else if (!_container.TryRemoveFromContainer(partUid, force: true))
                    {
                        stackToInsert = null;
                    }

                    if (stackToInsert != null)
                    {
                        if (!_container.Insert(stackToInsert.Value, machineFrame.PartContainer))
                            _container.Insert(stackToInsert.Value, storage.Container, force: true);
                        else
                        {
                            remainingMaterials[stack.StackTypeId] = Math.Max(0, materialRemaining - materialAmount);
                            changed = true;
                        }
                    }
                }
            }

            foreach (var (compName, _) in machineFrame.ComponentRequirements)
            {
                if (!remainingComponents.TryGetValue(compName, out var compRemaining) || compRemaining <= 0)
                    continue;

                var registration = _factory.GetRegistration(compName);
                if (!HasComp(partUid, registration.Type))
                    continue;

                if (!_container.TryRemoveFromContainer(partUid, force: true))
                    continue;

                if (!_container.Insert(partUid, machineFrame.PartContainer))
                {
                    _container.Insert(partUid, storage.Container, force: true);
                    continue;
                }

                remainingComponents[compName] = compRemaining - 1;
                changed = true;
                break;
            }

            if (!TryComp<TagComponent>(partUid, out var tagComp))
                continue;

            {
                foreach (var (tagName, _) in machineFrame.TagRequirements)
                {
                    if (!remainingTags.TryGetValue(tagName, out var tagRemaining) || tagRemaining <= 0)
                        continue;

                    if (!_tag.HasTag(tagComp, tagName))
                        continue;

                    if (!_container.TryRemoveFromContainer(partUid, force: true))
                        continue;

                    if (!_container.Insert(partUid, machineFrame.PartContainer))
                    {
                        _container.Insert(partUid, storage.Container, force: true);
                        continue;
                    }

                    remainingTags[tagName] = tagRemaining - 1;
                    changed = true;
                    break;
                }
            }
        }

        if (!changed)
            return false;

        _machineFrame.RegenerateProgress(machineFrame);

        if (_machineFrame.IsComplete(machineFrame))
            _popup.PopupEntity(Loc.GetString("machine-frame-component-on-complete"), frameUid, user);

        return true;
    }
}
