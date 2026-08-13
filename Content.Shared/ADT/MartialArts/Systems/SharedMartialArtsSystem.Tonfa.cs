using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Standing;

namespace Content.Shared.ADT.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializeTonfa()
    {
        SubscribeLocalEvent<WeaponMartialArtComponent, TonfaSolarPlexusPerformedEvent>(OnTonfaSolarPlexus);
        SubscribeLocalEvent<WeaponMartialArtComponent, TonfaRibsPerformedEvent>(OnTonfaRibs);
        SubscribeLocalEvent<WeaponMartialArtComponent, TonfaWristPerformedEvent>(OnTonfaWrist);
        SubscribeLocalEvent<WeaponMartialArtComponent, TonfaLegSweepPerformedEvent>(OnTonfaLegSweep);
        SubscribeLocalEvent<WeaponMartialArtComponent, TonfaGrabBreakPerformedEvent>(OnTonfaGrabBreak);
    }

    private void OnTonfaSolarPlexus(Entity<WeaponMartialArtComponent> ent, ref TonfaSolarPlexusPerformedEvent args)
    {
        if (!TryUseWeaponMartialArt(ent, out var proto, out var user, out var target, out _))
            return;

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage, source: user, with: ent.Owner);

        FinishWeaponCombo(ent, user, target, proto.Name, args.Sound);
    }

    private void OnTonfaRibs(Entity<WeaponMartialArtComponent> ent, ref TonfaRibsPerformedEvent args)
    {
        if (!TryUseWeaponMartialArt(ent, out var proto, out var user, out var target, out _))
            return;

        DoDamage(user, target, proto.DamageType, proto.ExtraDamage, out _);

        FinishWeaponCombo(ent, user, target, proto.Name, args.Sound);
    }

    private void OnTonfaWrist(Entity<WeaponMartialArtComponent> ent, ref TonfaWristPerformedEvent args)
    {
        if (!TryUseWeaponMartialArt(ent, out var proto, out var user, out var target, out _))
            return;

        DoDamage(user, target, proto.DamageType, proto.ExtraDamage, out _);

        if (_hands.TryGetActiveItem(target, out var activeItem))
            _hands.TryDrop(target, activeItem.Value);

        FinishWeaponCombo(ent, user, target, proto.Name, args.Sound);
    }

    private void OnTonfaLegSweep(Entity<WeaponMartialArtComponent> ent, ref TonfaLegSweepPerformedEvent args)
    {
        if (!TryUseWeaponMartialArt(ent, out var proto, out var user, out var target, out var downed))
            return;

        if (downed)
        {
            _movementMod.TryUpdateMovementSpeedModDuration(target,
                MartsGenericSlow,
                args.SlowdownTime,
                args.WalkSpeedModifier,
                args.SprintSpeedModifier);
        }
        else
        {
            _stun.TryKnockdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime), true, true, proto.DropItems);
        }

        FinishWeaponCombo(ent, user, target, proto.Name, args.Sound);
    }

    private void OnTonfaGrabBreak(Entity<WeaponMartialArtComponent> ent, ref TonfaGrabBreakPerformedEvent args)
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

        if (TryComp<StandingStateComponent>(target, out var standing) && standing.Standing)
            _stun.TryKnockdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime), true, true, proto.DropItems);

        var dir = _transform.GetMapCoordinates(target).Position - _transform.GetMapCoordinates(user).Position;
        _grabThrown.Throw(target, user, dir, proto.ThrownSpeed, behavior: proto.DropItems);

        FinishWeaponCombo(ent, user, target, proto.Name, args.Sound);
    }
}
