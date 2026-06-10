using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Shared.ADT.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializeCqc()
    {
        SubscribeLocalEvent<CanPerformComboComponent, CqcSlamPerformedEvent>(OnCQCSlam);
        SubscribeLocalEvent<CanPerformComboComponent, CqcKickPerformedEvent>(OnCQCKick);
        SubscribeLocalEvent<CanPerformComboComponent, CqcRestrainPerformedEvent>(OnCQCRestrain);
        SubscribeLocalEvent<CanPerformComboComponent, CqcPressurePerformedEvent>(OnCQCPressure);
        SubscribeLocalEvent<CanPerformComboComponent, CqcConsecutivePerformedEvent>(OnCQCConsecutive);

        SubscribeLocalEvent<MartialArtsKnowledgeComponent, CanDoCQCEvent>(OnCQCCheck);

        SubscribeLocalEvent<GrantCqcComponent, UseInHandEvent>(OnGrantCQCUse);
        SubscribeLocalEvent<GrantCqcComponent, MapInitEvent>(OnCQCMapInitEvent);
    }

    #region Generic Methods

    private void OnCQCCheck(Entity<MartialArtsKnowledgeComponent> ent, ref CanDoCQCEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.Blocked)
        {
            args.Handled = true;
            return;
        }

        foreach (var entInRange in _lookup.GetEntitiesInRange(ent, 8f))
        {
            if (!TryPrototype(entInRange, out var entProto) || entProto.ID != "SpawnPointChef")
                continue;

            args.Handled = true;
            return;
        }
    }

    private void OnCQCMapInitEvent(Entity<GrantCqcComponent> ent, ref MapInitEvent args)
    {
        if (!HasComp<MobStateComponent>(ent))
            return;

        if (!TryGrantMartialArt(ent, ent.Comp))
            return;

        if (TryComp<MartialArtsKnowledgeComponent>(ent, out var knowledge))
            knowledge.Blocked = true;
    }

    private void OnGrantCQCUse(EntityUid ent, GrantMartialArtKnowledgeComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_netManager.IsServer)
            return;

        if (!TryGrantMartialArt(args.User, comp))
            return;

        var coords = Transform(args.User).Coordinates;
        _audio.PlayPvs(comp.SoundOnUse, coords);
        if (comp.MultiUse)
            return;

        QueueDel(ent);
        if (comp.SpawnedProto == null)
            return;

        Spawn(comp.SpawnedProto, coords);
    }

    private void OnCQCAttackPerformed(Entity<MartialArtsKnowledgeComponent> ent, ref ComboAttackPerformedEvent args)
    {
        if (args.Weapon != args.Performer || args.Target == args.Performer)
            return;

        switch (args.Type)
        {
            case ComboAttackType.Disarm:
                _stamina.TakeStaminaDamage(args.Target, 25f, applyResistances: true);
                break;
            case ComboAttackType.Harm:
                // Leg sweep when user is downed
                if (!TryComp<StandingStateComponent>(ent.Owner, out var standing)
                    || standing.Standing
                    || !TryComp<StandingStateComponent>(args.Target, out var targetStanding)
                    || !targetStanding.Standing)
                    break;

                if (HasComp<KnockedDownComponent>(ent.Owner))
                    RemComp<KnockedDownComponent>(ent.Owner);

                _standingState.Stand(ent.Owner);
                _stun.TryKnockdown(args.Target, TimeSpan.FromSeconds(5), true);
                ComboPopup(ent, args.Target, "Leg Sweep");
                break;
        }
    }

    #endregion

    #region Combo Methods

    private void OnCQCSlam(Entity<CanPerformComboComponent> ent, ref CqcSlamPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed)
            || downed)
            return;

        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);
        _stun.TryKnockdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime), true, true, proto.DropItems);
        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnCQCKick(Entity<CanPerformComboComponent> ent, ref CqcKickPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        var mapPos = _transform.GetMapCoordinates(ent).Position;
        var hitPos = _transform.GetMapCoordinates(target).Position;
        var dir = hitPos - mapPos;
        dir *= 1f / dir.Length();

        if (downed)
        {
            if (TryComp<StatusEffectsComponent>(target, out var status))
                _status.TryAddStatusEffect<Content.Shared.Bed.Sleep.ForcedSleepingStatusEffectComponent>(
                    target, "ForcedSleep", TimeSpan.FromSeconds(10), true, status);
            DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);
            _stamina.TakeStaminaDamage(target, proto.StaminaDamage * 2 + 5, source: ent, applyResistances: true);
        }
        else
        {
            _stamina.TakeStaminaDamage(target, proto.StaminaDamage, source: ent, applyResistances: true);
        }

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);
        _pulling.Throw(target, ent, dir, proto.ThrownSpeed);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit2.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnCQCRestrain(Entity<CanPerformComboComponent> ent, ref CqcRestrainPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        _stun.TryKnockdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime), true, true, proto.DropItems);
        _stamina.TakeStaminaDamage(target, proto.StaminaDamage, source: ent, applyResistances: true);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnCQCPressure(Entity<CanPerformComboComponent> ent, ref CqcPressurePerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage, source: ent, applyResistances: true);

        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();

        if (!_hands.TryGetActiveItem(target, out var activeItem))
            return;
        if (!_hands.TryDrop(target, activeItem.Value))
            return;
        if (!_hands.TryGetEmptyHand(ent.Owner, out var emptyHand))
            return;
        if (!_hands.TryPickup(ent, activeItem.Value, emptyHand))
            return;
        _hands.SetActiveHand(ent.Owner, emptyHand);
    }

    private void OnCQCConsecutive(Entity<CanPerformComboComponent> ent, ref CqcConsecutivePerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);
        _stamina.TakeStaminaDamage(target, proto.StaminaDamage, source: ent, applyResistances: true);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    #endregion
}
