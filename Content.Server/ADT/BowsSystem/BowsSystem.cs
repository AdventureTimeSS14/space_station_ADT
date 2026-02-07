using System.Diagnostics.CodeAnalysis;
using Content.Shared.ADT.BowsSystem.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Microsoft.VisualBasic;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.ADT.BowsSystem;

public sealed partial class BowsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpendedBowsComponent, ShotAttemptedEvent>(OnShootAttemp);
        SubscribeLocalEvent<ExpendedBowsComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);
    }

    public void OnShootAttemp(Entity<ExpendedBowsComponent> bow,ref ShotAttemptedEvent args)
    {
        if(bow.Comp.StepOfTension==0)
            args.Cancel();
    }

    private void OnGunRefreshModifiers(Entity<ExpendedBowsComponent> bow, ref GunRefreshModifiersEvent args)
    {
        if (bow.Comp.StepOfTension==0)
            return;
        args.ProjectileSpeed = args.ProjectileSpeed * bow.Comp.StepOfTension;;
    }
}