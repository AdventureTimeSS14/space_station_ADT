using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.ADT.Eye;

public sealed class ADTBlurryVisionStatusEffectSystem : EntitySystem
{
    [Dependency] private BlurryVisionSystem _blurry = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBlurryVisionStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<ADTBlurryVisionStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<BlindableComponent, GetBlurEvent>(OnGetBlur);
    }

    private void OnApplied(Entity<ADTBlurryVisionStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.Removed = false;
        _blurry.UpdateBlurMagnitude(args.Target);
    }

    private void OnRemoved(Entity<ADTBlurryVisionStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        ent.Comp.Removed = true;
        _blurry.UpdateBlurMagnitude(args.Target);
    }

    private void OnGetBlur(Entity<BlindableComponent> ent, ref GetBlurEvent args)
    {
        if (!_status.TryEffectsWithComp<ADTBlurryVisionStatusEffectComponent>(ent, out var effects))
            return;

        var blur = 0f;
        foreach (var effect in effects)
        {
            if (!effect.Comp1.Removed)
                blur = MathF.Max(blur, effect.Comp1.Blur);
        }

        args.Blur += blur;
    }
}
