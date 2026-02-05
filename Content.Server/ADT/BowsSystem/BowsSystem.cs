using System.Diagnostics.CodeAnalysis;
using Content.Shared.ADT.BowsSystem.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Microsoft.VisualBasic;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.ADT.BowsSystem;

public sealed partial class BowsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpendedBowsComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ExpendedBowsComponent, ShotAttemptedEvent>(OnShootAttemp);
        SubscribeLocalEvent<ExpendedBowsComponent, GunShotEvent>(OnShoot);
    }

    public void OnUseInHand(Entity<ExpendedBowsComponent> bow, ref UseInHandEvent args)
    {
        if (!TryComp<ItemSlotsComponent>(bow, out var slots) && !slots.Slots.Value)
            return;
        bow.Comp.coldownStart = TimeSpan.CurTime+bow.Comp.coldown;
        while(bow.Comp.IsHolding && bow.Comp.StepOfTension!=3)
        {
            if (Timing.CurTime>bow.Comp.coldownStart)
                continue;
            bow.Comp.StepOfTension++;
            bow.Comp.coldownStart = TimeSpan.CurTime+bow.Comp.coldown;
        }
    }
    public void OnShootAttemp(Entity<ExpendedBowsComponent> bow, ShotAttemptedEvent args)
    {
        if(bow.Comp.StepOfTension==0 && bow.Comp.StepOfTension!=null)
            args.Cancel();
        else
        {
            gun.ProjectileSpeed = gun.ProjectileSpeed * bow.Comp.StepOfTension;
        }
    }

    public void OnShoot(Entity<ExpendedBowsComponent> bow, GunShotEvent args)
    {
        if(!TryComp<GunComponent>(bow, out var gun))
            return;
        if(!TryComp<ContainerAmmoProvider>(bow, out var containerComp))
            return;
        if (!Containers.TryGetContainer(containerComp.ProviderUid.Value, containerComp.Container, out var container))
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
                    args.Damage.DamageDict["Piercing"] = multipliedDamage;
                }
            }
        }
        bow.Comp.StepOfTension=0;
    }
}