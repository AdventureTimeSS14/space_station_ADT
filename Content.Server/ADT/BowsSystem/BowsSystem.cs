using Content.Shared.ADT.BowsSystem.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Server.ADT.BowsSystem;

public sealed partial class BowsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ExpendedBowsComponent, ShotAttemptedEvent>(OnShootAttempt);
    }

    public void OnShootAttempt(Entity<ExpendedBowsComponent> bow,ref ShotAttemptedEvent args)
    {
        if(bow.Comp.StepOfTension==0)
            args.Cancel();
    }
}