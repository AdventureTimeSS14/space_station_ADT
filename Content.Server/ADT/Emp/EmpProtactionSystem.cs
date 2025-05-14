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
using Content.Shared.ADT.Emp;

namespace Content.Server.ADT.EMPProtaction;

public sealed class EmpProtactionSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _slot = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<EmpContainerProtactionComponent, ItemSlotInsertAttemptEvent>(OnInserted);
        SubscribeLocalEvent<EmpContainerProtactionComponent, ItemSlotEjectedEvent>(OnEjected);
        SubscribeLocalEvent<EmpContainerProtactionComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EmpContainerProtactionComponent, MapInitEvent>(OnInit);
    }
    private void OnInserted(EntityUid uid, EmpContainerProtactionComponent component, ref ItemSlotInsertAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        EnsureComp<EmpProtectionComponent>(args.Item);
        component.BatteryUid = args.Item;
    }
    private void OnEjected(EntityUid uid, EmpContainerProtactionComponent component, ref ItemSlotEjectedEvent args)
    {
        if (args.Cancelled)
            return;
        RemComp<EmpProtectionComponent>(args.Item);
        component.BatteryUid = null;
    }
    private void OnShutdown(EntityUid uid, EmpContainerProtactionComponent component, ComponentShutdown args)
    {
        if (component.BatteryUid == null)
            return;
        RemComp<EmpProtectionComponent>(component.BatteryUid.Value);
    }
    private void OnInit(EntityUid uid, EmpContainerProtactionComponent component, MapInitEvent args)
    {
        var battery = _slot.GetItemOrNull(uid, component.ContainerId);
        if (battery == null)
            return;
        EnsureComp<EmpProtectionComponent>(battery.Value);
    }
}
