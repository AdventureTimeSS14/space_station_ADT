using Content.Shared.Blocking.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Mobs.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.ADT.ShieldBash;

public sealed class ADTShieldBashSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTShieldBashComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<ADTShieldBashComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        if (!_toggle.IsActivated(ent.Owner))
            return;

        var raised = TryComp<BlockingComponent>(ent, out var blocking) && blocking.IsRaised;
        var distance = raised ? ent.Comp.RaisedDistance : ent.Comp.Distance;
        var userPos = _transform.GetWorldPosition(args.User);

        foreach (var target in args.HitEntities)
        {
            if (target == args.User || !HasComp<MobStateComponent>(target))
                continue;

            var direction = _transform.GetWorldPosition(target) - userPos;
            if (direction.LengthSquared() <= 0f)
                continue;

            _throwing.TryThrow(target,
                direction.Normalized() * distance,
                ent.Comp.Speed,
                args.User,
                pushbackRatio: 0f,
                compensateFriction: true,
                doSpin: false,
                playSound: false);
        }
    }
}
