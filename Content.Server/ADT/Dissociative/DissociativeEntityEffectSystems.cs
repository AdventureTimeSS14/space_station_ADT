using Content.Server.ADT.Dissociative.Components;
using Content.Shared.ADT.Hallucinations.EntityEffects;
using Content.Shared.EntityEffects;

namespace Content.Server.ADT.Dissociative;

public sealed partial class DissociativeExtendFrontEntityEffectSystem : EntityEffectSystem<DissociativeIdentityComponent, DissociativeExtendFront>
{
    [Dependency] private DissociativeIdentitySystem _dissociative = default!;

    protected override void Effect(Entity<DissociativeIdentityComponent> entity, ref EntityEffectEvent<DissociativeExtendFront> args)
    {
        _dissociative.ExtendFront(entity, TimeSpan.FromSeconds(args.Effect.Seconds));
    }
}

public sealed partial class DissociativeForceFrontEntityEffectSystem : EntityEffectSystem<DissociativeIdentityComponent, DissociativeForceFront>
{
    [Dependency] private DissociativeIdentitySystem _dissociative = default!;

    protected override void Effect(Entity<DissociativeIdentityComponent> entity, ref EntityEffectEvent<DissociativeForceFront> args)
    {
        _dissociative.ForceFront(entity);
    }
}

public sealed partial class DissociativeSpeedCooldownEntityEffectSystem : EntityEffectSystem<DissociativeIdentityComponent, DissociativeSpeedCooldown>
{
    protected override void Effect(Entity<DissociativeIdentityComponent> entity, ref EntityEffectEvent<DissociativeSpeedCooldown> args)
    {
        entity.Comp.NextFront -= TimeSpan.FromSeconds(args.Effect.Seconds * args.Scale);
    }
}
