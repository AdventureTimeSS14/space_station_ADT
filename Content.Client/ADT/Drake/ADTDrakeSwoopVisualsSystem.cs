using System.Numerics;
using Content.Client.DamageState;
using Content.Shared.ADT.Drake;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.Maths;
using Robust.Shared.Analyzers;

namespace Content.Client.ADT.Drake;

public sealed class ADTDrakeSwoopVisualsSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const string AnimationKey = "adt-drake-swoop";
    private const string ShadowState = "shadow";
    private const string AliveState = "dragon";
    private const string DeadState = "dragon_dead";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDrakeSwoopComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ADTDrakeSwoopComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<ADTDrakeSwoopComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<ADTDrakeSwoopComponent> ent, ref ComponentStartup args)
    {
        ApplyPhase(ent);
    }

    private void OnHandleState(Entity<ADTDrakeSwoopComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ApplyPhase(ent);
    }

    private void OnShutdown(Entity<ADTDrakeSwoopComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent) || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        _animationPlayer.Stop(ent.Owner, null, AnimationKey);

        var dead = TryComp<MobStateComponent>(ent, out var mob) && mob.CurrentState == MobState.Dead;
        SetBaseState((ent, sprite), dead ? DeadState : AliveState);
        _sprite.SetColor((ent, sprite), sprite.Color.WithAlpha(1f));
        _sprite.SetScale((ent, sprite), Vector2.One);
    }

    private void ApplyPhase(Entity<ADTDrakeSwoopComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) || !TryComp<ADTDrakeComponent>(ent, out var drake))
            return;

        var spriteEnt = new Entity<SpriteComponent?>(ent, sprite);

        switch (ent.Comp.Phase)
        {
            case ADTDrakeSwoopPhase.Rising:
                SetBaseState(spriteEnt, ShadowState);
                Animate(ent, sprite, drake.SwoopRiseAlpha, drake.SwoopRiseScale, drake.SwoopRiseTime, Easings.OutBounce);
                break;

            case ADTDrakeSwoopPhase.Ascending:
                SetBaseState(spriteEnt, ShadowState);
                Animate(ent, sprite, drake.SwoopAirAlpha, drake.SwoopAirScale, drake.SwoopAscendTime);
                break;

            case ADTDrakeSwoopPhase.Chasing:
            case ADTDrakeSwoopPhase.Arena:
                SetBaseState(spriteEnt, ShadowState);
                _sprite.SetColor(spriteEnt, sprite.Color.WithAlpha(drake.SwoopAirAlpha));
                _sprite.SetScale(spriteEnt, new Vector2(drake.SwoopAirScale));
                break;

            case ADTDrakeSwoopPhase.Descending:
                SetBaseState(spriteEnt, ShadowState);
                Animate(ent, sprite, 1f, 1f, drake.SwoopDescentTime);
                break;

            case ADTDrakeSwoopPhase.Landed:
                _animationPlayer.Stop(ent.Owner, null, AnimationKey);
                SetBaseState(spriteEnt, AliveState);
                _sprite.SetColor(spriteEnt, sprite.Color.WithAlpha(1f));
                _sprite.SetScale(spriteEnt, Vector2.One);
                break;
        }
    }

    private void Animate(EntityUid uid, SpriteComponent sprite, float alpha, float scale, TimeSpan length, Func<float, float>? easing = null)
    {
        _animationPlayer.Stop(uid, null, AnimationKey);

        var seconds = (float) length.TotalSeconds;
        var animation = new Animation
        {
            Length = length,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Color, 0f),
                        new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(alpha), seconds, easing),
                    },
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Scale, 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(scale), seconds, easing),
                    },
                },
            },
        };

        _animationPlayer.Play(uid, animation, AnimationKey);
    }

    private void SetBaseState(Entity<SpriteComponent?> sprite, string state)
    {
        if (!_sprite.LayerMapTryGet(sprite, DamageStateVisualLayers.Base, out var layer, false))
            return;

        _sprite.LayerSetRsiState(sprite, layer, state);
    }
}
