using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializePtsd()
    {
        SubscribeLocalEvent<CanPerformComboComponent, PtsdLegSweepComboPerformedEvent>(OnPtsdLegSweepCombo);
        SubscribeLocalEvent<CanPerformComboComponent, PtsdArmLockBehindBackComboPerformedEvent>(OnPtsdArmLockBehindBackCombo);
        SubscribeLocalEvent<CanPerformComboComponent, PtsdBootKickComboPerformedEvent>(OnPtsdBootKickCombo);

        SubscribeLocalEvent<GrantPtsdComponent, UseInHandEvent>(OnGrantCQCUse);
    }

    private void OnPtsdLegSweepCombo(Entity<CanPerformComboComponent> ent, ref PtsdLegSweepComboPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed)
            || downed)
            return;

        _stun.TryKnockdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime), true, true, proto.DropItems);
        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, Loc.GetString("ptsd-combo-leg-sweep"));
        ent.Comp.LastAttacks.Clear();
    }

    private void OnPtsdArmLockBehindBackCombo(Entity<CanPerformComboComponent> ent, ref PtsdArmLockBehindBackComboPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage);
        ComboPopup(ent, target, Loc.GetString("ptsd-combo-arm-lock"));
        ent.Comp.LastAttacks.Clear();
    }

    private void OnPtsdBootKickCombo(Entity<CanPerformComboComponent> ent, ref PtsdBootKickComboPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        if (!downed)
        {
            _popupSystem.PopupEntity(Loc.GetString("martial-arts-fail-target-standing"), ent, ent);
            ent.Comp.LastAttacks.Clear();
            return;
        }

        var mapPos = _transform.GetMapCoordinates(ent).Position;
        var hitPos = _transform.GetMapCoordinates(target).Position;
        var dir = hitPos - mapPos;

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);
        _grabThrown.Throw(target, ent, dir, proto.ThrownSpeed, behavior: proto.DropItems);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, Loc.GetString("ptsd-combo-boot-kick"));
        ent.Comp.LastAttacks.Clear();
    }
}