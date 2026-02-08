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

namespace Content.Shared.ADT.BowsSystem;

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
        SubscribeLocalEvent<ExpendedBowsComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ExpendedBowsComponent, GunRefreshModifiersEvent>(EditSpeed);
    }
    
    public void OnShoot(Entity<ExpendedBowsComponent> bow, ref GunShotEvent args)
    {
        if(!TryComp<ContainerAmmoProviderComponent>(bow, out var containerComp))
            return;
        if (containerComp.ProviderUid.Value != null && !_containerSystem.TryGetContainer(containerComp.ProviderUid.Value, containerComp.Container, out var container))
            return;
        if (bow.Comp.StepOfTension!=bow.Comp.MinTension)
        {
            foreach (var i in container.ContainedEntities)
            {
                if (TryComp<ProjectileComponent>(i,out var proj))
                {
                    if (!proj.Damage.DamageDict.TryGetValue(bow.Comp.DamageToModifying, out var ProjectileDamage))
                        return;
                    var multipliedDamage = ProjectileDamage * bow.Comp.StepOfTension;
                    proj.Damage.DamageDict[bow.Comp.DamageToModifying] = multipliedDamage;
                }
            }
        }
        bow.Comp.StepOfTension=bow.Comp.MinTension;
    }

    public void OnUseInHand(Entity<ExpendedBowsComponent> bow, ref UseInHandEvent args)
    {
        bow.Comp.coldownStart = _timing.CurTime + TimeSpan.FromSeconds(bow.Comp.floatToColdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ExpendedBowsComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.coldownStart)
                continue;
            if(!TryComp<WieldableComponent>(uid,out var wielded) || !wielded.Wielded)
            {
                comp.StepOfTension=comp.MinTension;
                continue;
            }
            if (comp.StepOfTension >= comp.MaxTension)
                continue;

            comp.StepOfTension++;
            _popup.PopupClient(Loc.GetString(comp.TensionAndLoc[comp.StepOfTension],("user", wielded.User)), uid, wielded.User);
            comp.coldownStart = _timing.CurTime + TimeSpan.FromSeconds(comp.floatToColdown);
        }
    }

    public void EditSpeed(Entity<ExpendedBowsComponent> bow, ref GunRefreshModifiersEvent args)
    {
        args.ProjectileSpeed *= bow.Comp.TensionAndModieferSpeed[bow.Comp.StepOfTension];
    }
}