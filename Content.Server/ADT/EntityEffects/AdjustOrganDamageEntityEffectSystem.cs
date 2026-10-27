using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Robust.Shared.Timing;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class AdjustOrganDamageEntityEffectSystem : EntityEffectSystem<BodyComponent, AdjustOrganDamage>
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private OrganHealthSystem _organHealth = default!;

    protected override void Effect(Entity<BodyComponent> entity, ref EntityEffectEvent<AdjustOrganDamage> args)
    {
        var amount = args.Effect.Amount * args.Scale;

        if (args.Effect.Category is { } category)
        {
            _organHealth.ChangeDamage(entity, category, amount, args.Effect.Kind);
            return;
        }

        foreach (var organ in _body.EnumerateOrgans<OrganHealthComponent>((entity.Owner, entity.Comp)))
        {
            _organHealth.ChangeDamage((organ.Owner, organ.Comp2), amount, args.Effect.Kind);
        }
    }
}

public sealed partial class SuppressHeartAttackEntityEffectSystem : EntityEffectSystem<OrganFunctionsComponent, SuppressHeartAttack>
{
    [Dependency] private IGameTiming _timing = default!;

    protected override void Effect(Entity<OrganFunctionsComponent> entity, ref EntityEffectEvent<SuppressHeartAttack> args)
    {
        var until = _timing.CurTime + args.Effect.Duration;
        if (until > entity.Comp.HeartAttackSuppressedUntil)
            entity.Comp.HeartAttackSuppressedUntil = until;
    }
}
