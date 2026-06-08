using Content.Shared.ADT.BerserkImplant;
using Content.Shared.ADT.Trail;

namespace Content.Server.ADT.BerserkImplant;

public sealed class BerserkImplantSystem : SharedBerserkImplantSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BerserkImplantActiveComponent, ComponentInit>(OnActiveInit);
        SubscribeLocalEvent<BerserkImplantActiveComponent, ComponentRemove>(OnRemove);
    }

    private void OnActiveInit(Entity<BerserkImplantActiveComponent> ent, ref ComponentInit args)
    {
        EnsureComp<BerserkVisionComponent>(ent.Owner);

        if (!HasComp<TrailComponent>(ent.Owner))
        {
            var trail = AddComp<TrailComponent>(ent.Owner);
            trail.RenderedEntity = ent.Owner;
            trail.LerpTime = 0.05f;
            trail.LerpDelay = TimeSpan.FromSeconds(0.05);
            trail.Lifetime = 0.25f;
            trail.Frequency = 0.07f;
            trail.AlphaLerpAmount = 0.5f;
            trail.MaxParticleAmount = 12;
            trail.Color = new Color(220, 25, 25, 200);
        }
    }

    private void OnRemove(Entity<BerserkImplantActiveComponent> ent, ref ComponentRemove args)
    {
        Damageable.TryChangeDamage(ent.Owner, ent.Comp.DelayedDamage * ent.Comp.DelayedDamageModifier, true);
        RemComp<BerserkVisionComponent>(ent.Owner);
        RemComp<TrailComponent>(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;
        var query = EntityQueryEnumerator<BerserkImplantActiveComponent>();

        while (query.MoveNext(out var ent, out var Berserk))
        {
            if (Berserk.EndTime > curTime)
                continue;

            RemCompDeferred<BerserkImplantActiveComponent>(ent);
        }
    }
}
