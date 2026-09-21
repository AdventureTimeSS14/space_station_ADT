using Content.Shared.Movement.Pulling.Components;

namespace Content.Shared.ADT.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializeSyndicateAxe()
    {
        SubscribeLocalEvent<WeaponMartialArtComponent, SyndicateAxeKnockdownPerformedEvent>(OnSyndicateAxeKnockdown);
        SubscribeLocalEvent<WeaponMartialArtComponent, SyndicateAxeGrabPushPerformedEvent>(OnSyndicateAxeGrabPush);
    }

    private void OnSyndicateAxeKnockdown(Entity<WeaponMartialArtComponent> ent,
        ref SyndicateAxeKnockdownPerformedEvent args)
    {
        if (!TryUseWeaponMartialArt(ent, out var proto, out var user, out var target, out var downed))
            return;

        if (downed)
        {
            _popupSystem.PopupClient(Loc.GetString("martial-arts-fail-target-down"), user, user);
            ResetWeaponCombo(ent, false);
            return;
        }

        _stun.TryKnockdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime), true, true, proto.DropItems);

        FinishWeaponCombo(ent, user, target, proto.Name, args.Sound);
    }

    private void OnSyndicateAxeGrabPush(Entity<WeaponMartialArtComponent> ent,
        ref SyndicateAxeGrabPushPerformedEvent args)
    {
        if (!TryUseWeaponMartialArt(ent, out var proto, out var user, out var target, out _))
            return;

        if (!TryComp<PullerComponent>(target, out var targetPuller)
            || targetPuller.Pulling != user)
        {
            _popupSystem.PopupClient(Loc.GetString("martial-arts-fail-not-grabbed"), user, user);
            ResetWeaponCombo(ent, false);
            return;
        }

        if (TryComp<PullableComponent>(user, out var selfPullable))
            _pulling.TryStopPull(user, selfPullable, target, true);

        var dir = _transform.GetMapCoordinates(target).Position - _transform.GetMapCoordinates(user).Position;
        _grabThrown.Throw(target, user, dir, proto.ThrownSpeed, behavior: proto.DropItems);

        _movementMod.TryUpdateMovementSpeedModDuration(target,
            MartsGenericSlow,
            args.SlowdownTime,
            args.WalkSpeedModifier,
            args.SprintSpeedModifier);

        FinishWeaponCombo(ent, user, target, proto.Name, args.Sound);
    }
}
