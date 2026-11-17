// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Content.Shared.Hands;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;

namespace Content.Server.Interaction;

public sealed partial class DeleteOnDropSystem : EntitySystem
{
    [Dependency] private IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeleteOnDropComponent, GotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<DeleteOnDropComponent, GotUnequippedHandEvent>(OnUnequipHand);
        SubscribeLocalEvent<DeleteOnDropComponent, DroppedEvent>(OnDropped);
    }

    private void OnUnequip(EntityUid uid, DeleteOnDropComponent item, GotUnequippedEvent args)
    {
        if (!item.DeleteOnDrop || !_entityManager.EntityExists(uid))
            return;

        QueueDel(uid);
    }

    private void OnUnequipHand(EntityUid uid, DeleteOnDropComponent item, GotUnequippedHandEvent args)
    {
        if (!item.DeleteOnDrop || !_entityManager.EntityExists(uid))
            return;

        QueueDel(uid);
    }

    private void OnDropped(EntityUid uid, DeleteOnDropComponent item, DroppedEvent args)
    {
        if (!item.DeleteOnDrop || !_entityManager.EntityExists(uid))
            return;

        QueueDel(uid);
    }
}

