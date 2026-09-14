using Content.Shared.ADT.Lavaland.LegionCore;
using Content.Shared.Damage.Systems;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Lavaland.LegionCore;

public sealed class ADTRegenerativeCoreStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRegenerativeCoreStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<ADTRegenerativeCoreStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
    }

    private void OnApplied(Entity<ADTRegenerativeCoreStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.Target = args.Target;
        ent.Comp.NextTick = _timing.CurTime + ent.Comp.Interval;
    }

    private void OnRemoved(Entity<ADTRegenerativeCoreStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        ent.Comp.Target = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTRegenerativeCoreStatusEffectComponent>();

        while (query.MoveNext(out _, out var comp))
        {
            if (comp.Target is not { } target || TerminatingOrDeleted(target))
                continue;

            if (comp.NextTick > curTime)
                continue;

            comp.NextTick = curTime + comp.Interval;
            _damageable.TryChangeDamage(target, comp.Healing, true, false);
        }
    }
}
