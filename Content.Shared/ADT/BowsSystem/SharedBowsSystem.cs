using Content.Shared.ADT.BowsSystem.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction.Events;
using Content.Shared.Projectiles;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using System.Reflection.Metadata;
using System.Diagnostics;
using System.Threading;

namespace Content.Server.ADT.BowsSystem;

public sealed partial class BowsSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExpendedBowsComponent, GunShotEvent>(OnShoot);
    }
    
    public void OnShoot(Entity<ExpendedBowsComponent> bow, ref GunShotEvent args)
    {
        if(!TryComp<ContainerAmmoProviderComponent>(bow, out var containerComp))
            return;
        if (_containerSystem.TryGetContainer(containerComp.ProviderUid.Value, containerComp.Container, out var container))
            return;
        if (bow.Comp.StepOfTension!=0)
        {
            foreach (var i in container.ContainedEntities)
            {
                if (TryComp<ProjectileComponent>(i,out var proj))
                {
                    if (!proj.Damage.DamageDict.TryGetValue("Piercing", out var ProjectileDamage))
                        return;
                    var multipliedDamage = ProjectileDamage * bow.Comp.StepOfTension;
                    proj.Damage.DamageDict["Piercing"] = multipliedDamage;
                }
            }
        }
    }

public override void Update(float frameTime)
{
    base.Update(frameTime);

    var query = EntityQueryEnumerator<ExpendedBowsComponent>();

    while (query.MoveNext(out var uid, out var comp))
    {
        comp.coldownStart = _timing.CurTime + comp.coldown;
        if(!TryComp<WieldableComponent>(uid,out var wielded))
        {
            comp.StepOfTension=0;
            return;
        }
        if (comp.StepOfTension >= 3)
            continue;

        if (_timing.CurTime < comp.coldownStart)
            continue;

        comp.StepOfTension++;
         _popup.PopupClient(Loc.GetString(comp.TensionAndLoc[comp.StepOfTension],("user", wielded.User)), uid, wielded.User);
        comp.coldownStart = _timing.CurTime + comp.coldown;
    }
}

}