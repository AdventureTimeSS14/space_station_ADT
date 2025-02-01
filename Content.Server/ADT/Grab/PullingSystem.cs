using System.Numerics;
using Content.Server.Popups;
using Content.Shared.ADT.Grab;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.ADT.Pulling.Systems;

/// <summary>
/// Allows one entity to pull another behind them via a physics distance joint.
/// </summary>
public sealed partial class PullingSystem : SharedPullingSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override bool TryIncreaseGrabStage(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        if (!base.TryIncreaseGrabStage(puller, pullable))
            return false;

        puller.Comp.Stage++;
        // Switch the popup type based on the new grab stage
        var popupType = PopupType.Small;
        switch (puller.Comp.Stage)
        {
            case GrabStage.Soft:
                popupType = PopupType.Small;
                break;
            case GrabStage.Hard:
                popupType = PopupType.SmallCaution;
                break;
            case GrabStage.Choke:
                popupType = PopupType.MediumCaution;
                break;
            default:
                popupType = PopupType.Small;
                break;
        }

        // Do grab stage change effects
        _popup.PopupPredicted(Loc.GetString($"grab-increase-{puller.Comp.Stage.ToString().ToLower()}-popup"), pullable, puller, popupType);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), pullable, puller);
        Dirty(puller);

        return true;
    }

    public override bool TryLowerGrabStage(Entity<PullerComponent> puller, Entity<PullableComponent> pullable, bool ignoreTimings = false, bool effects = true)
    {
        if (!base.TryLowerGrabStage(puller, pullable, ignoreTimings, effects))
            return false;
        puller.Comp.Stage--;

        // Do grab stage change effects
        if (effects)
        {
            _popup.PopupPredicted(Loc.GetString($"grab-lower-{puller.Comp.Stage.ToString().ToLower()}-popup"), pullable, puller, PopupType.Small);
            _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), pullable, puller);
        }

        Dirty(puller);

        return true;
    }

    public override void Throw(Entity<PullerComponent> puller, Entity<PullableComponent> pullable, EntityCoordinates coords)
    {
        base.Throw(puller, pullable, coords);

        var xform = Transform(pullable);
        var startCoords = _transform.ToMapCoordinates(xform.Coordinates);
        var targCoords = _transform.ToMapCoordinates(coords);
        Vector2 vector = Vector2.Clamp(targCoords.Position - startCoords.Position, new(-2.5f), new(2.5f));
        Vector2 selfVec = Vector2.Clamp(new(-vector.X, -vector.Y), new(-0.25f), new(0.25f));


        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), pullable);
        EnsureComp<GrabThrownComponent>(pullable);
        _throwing.TryThrow(pullable, vector, 8, animated: false, playSound: false, doSpin: false);
        _throwing.TryThrow(puller, selfVec, 5, animated: false, playSound: false, doSpin: false);
    }
}
