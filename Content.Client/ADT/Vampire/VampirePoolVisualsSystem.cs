using Content.Shared.ADT.Vampire.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.Vampire;

public sealed class VampirePoolVisualsSystem : EntitySystem
{
    [Dependency] private AnimationPlayerSystem _animation = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const string AnimationKey = "vampire-pool-transition";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampirePoolComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VampirePoolComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<VampirePoolComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnStartup(Entity<VampirePoolComponent> ent, ref ComponentStartup args)
    {
        SetLayers(ent, false);
        UpdateVisuals(ent);
    }

    private void OnHandleState(Entity<VampirePoolComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateVisuals(ent);
    }

    private void UpdateVisuals(Entity<VampirePoolComponent> ent)
    {
        if (ent.Comp.VisualHidden == ent.Comp.Hidden)
            return;

        ent.Comp.VisualHidden = ent.Comp.Hidden;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.LayerSetVisible((ent.Owner, sprite), ent.Comp.BodyLayer, false);
        _sprite.LayerSetVisible((ent.Owner, sprite), ent.Comp.EffectLayer, true);

        var duration = (float) ent.Comp.TransitionDuration.TotalSeconds;
        var flick = new AnimationTrackSpriteFlick
        {
            LayerKey = ent.Comp.EffectLayer,
        };

        if (ent.Comp.Hidden)
        {
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.MergeState, 0f));
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.HiddenState, duration));
        }
        else
        {
            flick.KeyFrames.Add(new AnimationTrackSpriteFlick.KeyFrame(ent.Comp.EmergeState, 0f));
        }

        var animation = new Animation
        {
            Length = ent.Comp.TransitionDuration + TimeSpan.FromSeconds(0.01),
            AnimationTracks =
            {
                flick,
            },
        };

        _animation.Stop(ent.Owner, AnimationKey);
        _animation.Play(ent.Owner, animation, AnimationKey);
    }

    private void OnAnimationCompleted(Entity<VampirePoolComponent> ent, ref AnimationCompletedEvent args)
    {
        if (args.Key != AnimationKey || !args.Finished || ent.Comp.Hidden)
            return;

        SetLayers(ent, false);
    }

    private void SetLayers(Entity<VampirePoolComponent> ent, bool hidden)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _sprite.LayerSetVisible((ent.Owner, sprite), ent.Comp.BodyLayer, !hidden);
        _sprite.LayerSetVisible((ent.Owner, sprite), ent.Comp.EffectLayer, hidden);
    }
}
