using Content.Shared.Gravity;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Components;
using Robust.Shared.Containers;

namespace Content.Shared.ADT.Weapons.Melee.ThrowOnHit;

public sealed class ADTThrowOnHitRestrictionsSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTThrowOnHitRestrictionsComponent, AttemptMeleeThrowOnHitEvent>(OnAttemptThrow);
    }

    private void OnAttemptThrow(Entity<ADTThrowOnHitRestrictionsComponent> ent, ref AttemptMeleeThrowOnHitEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.MobsOnly && !HasComp<MobStateComponent>(args.Target))
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.BlockFromContainer && args.User is { } user && _container.IsEntityInContainer(user))
        {
            args.Cancelled = true;
            return;
        }

        if (ent.Comp.BlockWeightless && (Transform(args.Target).GridUid == null || _gravity.IsWeightless(args.Target)))
            args.Cancelled = true;
    }
}
