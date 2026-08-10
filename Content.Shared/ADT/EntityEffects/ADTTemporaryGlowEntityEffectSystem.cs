using Content.Shared.EntityEffects;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.EntityEffects;

public sealed class ADTTemporaryGlowEntityEffectSystem : EntityEffectSystem<MetaDataComponent, ADTTemporaryGlow>
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ADTTemporaryGlow> args)
    {
        var effect = args.Effect;

        if (!TryComp<ADTTemporaryGlowComponent>(entity, out var glow))
        {
            glow = AddComp<ADTTemporaryGlowComponent>(entity);
            glow.HadLight = _light.TryGetLight(entity, out _);
        }

        glow.ExpiresAt = _timing.CurTime + effect.Duration;
        Dirty(entity.Owner, glow);

        if (glow.HadLight)
            return;

        var light = _light.EnsureLight(entity);
        _light.SetRadius(entity, effect.Radius, light);
        _light.SetColor(entity, effect.Color, light);
        _light.SetCastShadows(entity, false, light);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTTemporaryGlowComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (now < comp.ExpiresAt)
                continue;

            if (!comp.HadLight)
                _light.RemoveLightDeferred(uid);

            RemComp<ADTTemporaryGlowComponent>(uid);
        }
    }
}

public sealed partial class ADTTemporaryGlow : EntityEffectBase<ADTTemporaryGlow>
{
    [DataField]
    public float Radius = 2f;

    [DataField]
    public Color Color = Color.White;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);
}
