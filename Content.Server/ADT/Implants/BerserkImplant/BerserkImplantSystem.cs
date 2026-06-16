using Content.Shared.ADT.Implants.BerserkImplant;
using Content.Shared.ADT.Trail;
using Content.Shared.ADT.Implants;

namespace Content.Server.ADT.Implants.BerserkImplant;

public sealed class BerserkImplantSystem : SharedBerserkImplantSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BerserkImplantActiveComponent, ComponentInit>(OnActiveInit);
        SubscribeLocalEvent<BerserkImplantActiveComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<BerserkImplantActiveComponent, ImplantEmpAffectedEvent>(OnEmp);
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
        RemCompDeferred<BerserkVisionComponent>(ent.Owner);
        RemCompDeferred<TrailComponent>(ent.Owner);
    }

    private void OnEmp(Entity<BerserkImplantActiveComponent> ent, ref ImplantEmpAffectedEvent args)
    {
        RemCompDeferred<BerserkImplantActiveComponent>(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = Timing.CurTime;
        var query = EntityQueryEnumerator<BerserkImplantActiveComponent>();

        while (query.MoveNext(out var ent, out var berserk))
        {
            if (berserk.EndTime > curTime)
                continue;

            RemCompDeferred<BerserkImplantActiveComponent>(ent);
        }
    }
}
