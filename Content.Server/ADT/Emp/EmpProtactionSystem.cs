using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Utility;
using Content.Server.Power.Components;
using Content.Server.Emp;
using Content.Server.PowerCell;
using Content.Shared.Examine;
using Content.Shared.Power;
using Content.Shared.PowerCell.Components;
using Content.Shared.Emp;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Storage.Components;
using Robust.Server.Containers;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.ADT.Emp;

namespace Content.Server.ADT.EMPProtaction;

/// <summary>
/// This handles the administrative test arena maps, and loading them.
/// </summary>
public sealed class EmpProtactionSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<EmpContainerProtactionComponent, ItemSlotInsertAttemptEvent>(OnInserted);
        SubscribeLocalEvent<EmpContainerProtactionComponent, ItemSlotEjectedEvent>(OnEjected);
    }
    private void OnInserted(EntityUid uid, EmpContainerProtactionComponent component, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        EnsureComp<EmpProtectionComponent>(args.Item);
    }
    private void OnEjected(EntityUid uid, EmpContainerProtactionComponent component, ref ItemSlotEjectedEvent args)
    {
        if (args.Cancelled)
            return;
        RemComp<EmpProtectionComponent>(args.Item);
    }
}
