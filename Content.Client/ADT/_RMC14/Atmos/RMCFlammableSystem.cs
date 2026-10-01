// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License

using Content.Shared._RMC14.Atmos;
using Content.Shared.Mobs;
using Content.Shared.Standing;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._RMC14.Atmos;

public sealed class RMCFlammableSystem : SharedRMCFlammableSystem
{
    private const string RollKey = "StopDropRollAnimation";
    private const float QuarterTurnTime = 0.25f;

    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RMCStopDropRollVisualsNetworkEvent>(OnResist);

        SubscribeLocalEvent<RMCStopDropRollVisualsComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RMCStopDropRollVisualsComponent, StoodEvent>(OnStood);
    }

    private void OnResist(RMCStopDropRollVisualsNetworkEvent ev)
    {
        if (!TryGetEntity(ev.User, out var user) || !HasComp<RMCStopDropRollVisualsComponent>(user))
            return;

        if (_animation.HasRunningAnimation(user.Value, RollKey))
            return;

        _animation.Play(user.Value, GetRollAnimation(ev.Length), RollKey);
    }

    private void OnMobStateChanged(Entity<RMCStopDropRollVisualsComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Alive)
            return;

        _animation.Stop(ent.Owner, RollKey);
    }

    private void OnStood(Entity<RMCStopDropRollVisualsComponent> ent, ref StoodEvent args)
    {
        _animation.Stop(ent.Owner, RollKey);
    }

    private static Animation GetRollAnimation(TimeSpan length)
    {
        var track = new AnimationTrackComponentProperty
        {
            ComponentType = typeof(TransformComponent),
            Property = nameof(TransformComponent.LocalRotation),
            InterpolationMode = AnimationInterpolationMode.Linear,
        };

        track.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(Angle.Zero, 0f));

        var quarters = Math.Max(1, (int) Math.Round(length.TotalSeconds / QuarterTurnTime));
        for (var i = 1; i <= quarters; i++)
        {
            var angle = Angle.FromDegrees(90 * (i % 4));
            track.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(angle, QuarterTurnTime));
        }

        while (track.KeyFrames.Count % 4 != 1)
        {
            var angle = Angle.FromDegrees(90 * (track.KeyFrames.Count % 4));
            track.KeyFrames.Add(new AnimationTrackProperty.KeyFrame(angle, QuarterTurnTime));
        }

        return new Animation
        {
            Length = TimeSpan.FromSeconds((track.KeyFrames.Count - 1) * QuarterTurnTime),
            AnimationTracks = { track },
        };
    }
}
