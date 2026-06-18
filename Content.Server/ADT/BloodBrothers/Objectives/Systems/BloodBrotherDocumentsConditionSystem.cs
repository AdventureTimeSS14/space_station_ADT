// SPDX-FileCopyrightText: 2026 Kirill Golubenko
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT
//
// Author: Kirill Golubenko
// Discord: golub4ik
// Ckey: WikiHampter

using Content.Server.ADT.BloodBrothers.Objectives.Components;
using Content.Shared.ADT.BloodBrothers.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.BloodBrothers.Objectives.Systems;

public sealed class BloodBrotherDocumentsConditionSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    private EntityQuery<BloodBrotherDocumentsComponent> _docQuery;
    private EntityQuery<ContainerManagerComponent> _containerQuery;
    private EntityQuery<ItemSlotsComponent> _itemSlotsQuery;
    private EntityQuery<HandsComponent> _handsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _docQuery = GetEntityQuery<BloodBrotherDocumentsComponent>();
        _containerQuery = GetEntityQuery<ContainerManagerComponent>();
        _itemSlotsQuery = GetEntityQuery<ItemSlotsComponent>();
        _handsQuery = GetEntityQuery<HandsComponent>();

        SubscribeLocalEvent<BloodBrotherDocumentsConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(EntityUid uid, BloodBrotherDocumentsConditionComponent comp, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = GetProgress(comp);
    }

    private float GetProgress(BloodBrotherDocumentsConditionComponent comp)
    {
        var teamHasRequired = false;
        var teamHasOwn = false;

        foreach (var mindId in comp.TeamMinds)
        {
            if (!TryComp<MindComponent>(mindId, out var mind) || mind.OwnedEntity == null)
                continue;

            var playerEntity = mind.OwnedEntity.Value;

            if (HasPrototypeInInventory(playerEntity, comp.RequiredFolder))
                teamHasRequired = true;

            if (HasPrototypeInInventory(playerEntity, comp.StartingFolder))
                teamHasOwn = true;
        }

        switch (comp.Variant)
        {
            case 0:
                return teamHasRequired && !teamHasOwn ? 1f : 0f;
            case 1:
                return teamHasRequired ? 1f : 0f;
            case 2:
                return teamHasOwn ? 1f : 0f;
            case 3:
                return !teamHasOwn ? 1f : 0f;
            case 4:
                return teamHasRequired ? 1f : 0f;
            default:
                return 0f;
        }
    }

    private bool HasPrototypeInInventory(EntityUid playerEntity, EntProtoId protoId)
    {
        var protoIdStr = protoId.ToString();
        var processed = new HashSet<EntityUid>();
        var toCheck = new Stack<EntityUid>();

        toCheck.Push(playerEntity);

        while (toCheck.TryPop(out var uid))
        {
            if (!processed.Add(uid))
                continue;

            if (MetaData(uid).EntityPrototype?.ID == protoIdStr)
                return true;

            if (_containerQuery.TryComp(uid, out var containerManager))
            {
                foreach (var container in containerManager.Containers.Values)
                {
                    foreach (var contained in container.ContainedEntities)
                    {
                        toCheck.Push(contained);
                    }
                }
            }

            if (_itemSlotsQuery.TryComp(uid, out var itemSlots))
            {
                foreach (var slot in itemSlots.Slots.Values)
                {
                    if (slot.Item.HasValue)
                        toCheck.Push(slot.Item.Value);
                }
            }

            if (_handsQuery.TryComp(uid, out var hands))
            {
                foreach (var held in _handsSystem.EnumerateHeld((uid, hands)))
                {
                    toCheck.Push(held);
                }
            }
        }

        return false;
    }
}
