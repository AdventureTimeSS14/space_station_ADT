using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared.ADT.Grab;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Content.Shared.Tag;
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
public sealed partial class ServerPullingSystem : PullingSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PullableComponent, SpeakAttemptEvent>(OnPullableSpeakAttempt);
    }

    private void OnPullableSpeakAttempt(EntityUid uid, PullableComponent comp, SpeakAttemptEvent args)
    {
        if (!TryComp<PullerComponent>(comp.Puller, out var puller) || puller.Stage < GrabStage.Choke)
            return;
        if (_tag.HasTag(uid, "ADTIgnoreChokeSpeechBlocking"))
            return;

        // Ran only on server so we dont care about prediction
        _popup.PopupEntity(Loc.GetString("grab-speech-attempt-choke"), uid, uid, PopupType.SmallCaution);
        args.Cancel();
    }

    public override bool TryIncreaseGrabStage(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        if (!base.TryIncreaseGrabStage(puller, pullable))
            return false;

        puller.Comp.Stage++;
        // Switch the popup type based on the new grab stage
        var popupType = puller.Comp.Stage switch
        {
            GrabStage.Soft => PopupType.Small,
            GrabStage.Hard => PopupType.SmallCaution,
            GrabStage.Choke => PopupType.MediumCaution,
            _ => PopupType.Small,
        };

        // Do grab stage change effects
        _popup.PopupPredicted(
                                Loc.GetString($"grab-increase-{puller.Comp.Stage.ToString().ToLower()}-popup-self",
                                                ("target", Identity.Entity(pullable, EntityManager))),
                                Loc.GetString($"grab-increase-{puller.Comp.Stage.ToString().ToLower()}-popup-others",
                                                ("target", Identity.Entity(pullable, EntityManager)),
                                                ("puller", Identity.Entity(pullable, EntityManager))),
                                pullable, puller, popupType);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), pullable, puller);
        Dirty(puller);

        _adminLogger.Add(LogType.Grab, LogImpact.Low,
            $"{ToPrettyString(puller):user} increased grab stage to {puller.Comp.Stage} while grabbing {ToPrettyString(pullable):target}");

        return true;
    }

    public override bool TryLowerGrabStage(Entity<PullerComponent> puller, Entity<PullableComponent> pullable, EntityUid user)
    {
        if (!base.TryLowerGrabStage(puller, pullable, user))
            return false;
        puller.Comp.Stage--;

        // Do grab stage change effects
        if (user != pullable.Owner)
        {
            _popup.PopupPredicted(
                                    Loc.GetString($"grab-lower-{puller.Comp.Stage.ToString().ToLower()}-popup-self",
                                                    ("target", Identity.Entity(pullable, EntityManager))),
                                    Loc.GetString($"grab-lower-{puller.Comp.Stage.ToString().ToLower()}-popup-others",
                                                    ("target", Identity.Entity(pullable, EntityManager)),
                                                    ("puller", Identity.Entity(pullable, EntityManager))),
                                    pullable, puller, PopupType.Small);
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

        _adminLogger.Add(LogType.Grab, LogImpact.Low,
            $"{ToPrettyString(puller):user} thrown {ToPrettyString(pullable):target} by grab");
    }
}
