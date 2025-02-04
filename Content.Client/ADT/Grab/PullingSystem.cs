using Content.Client.Effects;
using Content.Client.Popups;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Client.ADT.Pulling.Systems;

/// <summary>
/// Allows one entity to pull another behind them via a physics distance joint.
/// </summary>
public sealed partial class ClientPullingSystem : PullingSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ColorFlashEffectSystem _color = default!;

    public override bool TryIncreaseGrabStage(Entity<PullerComponent> puller, Entity<PullableComponent> pullable)
    {
        if (!base.TryIncreaseGrabStage(puller, pullable))
            return false;

        var targetStage = puller.Comp.Stage + 1;
        // Switch the popup type based on the new grab stage
        var popupType = targetStage switch
        {
            GrabStage.Soft => PopupType.Small,
            GrabStage.Hard => PopupType.SmallCaution,
            GrabStage.Choke => PopupType.MediumCaution,
            _ => PopupType.Small,
        };

        // Do grab stage change effects
        _popup.PopupPredicted(
                                Loc.GetString($"grab-increase-{targetStage.ToString().ToLower()}-popup-self",
                                                ("target", Identity.Entity(pullable, EntityManager))),
                                Loc.GetString($"grab-increase-{targetStage.ToString().ToLower()}-popup-others",
                                                ("target", Identity.Entity(pullable, EntityManager)),
                                                ("puller", Identity.Entity(pullable, EntityManager))),
                                pullable, puller, popupType);
        _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), pullable, puller);
        _color.RaiseEffect(Color.Yellow, new List<EntityUid>() { pullable.Owner }, Filter.Pvs(pullable.Owner));

        return true;
    }

    public override bool TryLowerGrabStage(Entity<PullerComponent> puller, Entity<PullableComponent> pullable, EntityUid user)
    {
        if (!base.TryLowerGrabStage(puller, pullable, user))
            return false;

        var targetStage = puller.Comp.Stage - 1;
        // Do grab stage change effects
        if (user != pullable.Owner)
        {
            _popup.PopupPredicted(
                                    Loc.GetString($"grab-lower-{targetStage.ToString().ToLower()}-popup-self",
                                                    ("target", Identity.Entity(pullable, EntityManager))),
                                    Loc.GetString($"grab-lower-{targetStage.ToString().ToLower()}-popup-others",
                                                    ("target", Identity.Entity(pullable, EntityManager)),
                                                    ("puller", Identity.Entity(pullable, EntityManager))),
                                    pullable, puller, PopupType.Small);
            _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg"), pullable, puller);
        }

        return true;
    }
}
