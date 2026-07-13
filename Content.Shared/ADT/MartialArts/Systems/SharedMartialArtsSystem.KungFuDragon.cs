// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.ADT.Grab;
using Content.Shared.ADT.MartialArts;
using Content.Shared.CombatMode;
using Content.Shared.Emoting;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Physics.Components;

namespace Content.Shared.ADT.MartialArts;

public abstract partial class SharedMartialArtsSystem
{
    private void InitializeDragon()
    {
        SubscribeLocalEvent<CanPerformComboComponent, DragonClawPerformedEvent>(OnDragonClaw);
        SubscribeLocalEvent<CanPerformComboComponent, DragonTailPerformedEvent>(OnDragonTail);
        SubscribeLocalEvent<CanPerformComboComponent, DragonStrikePerformedEvent>(OnDragonStrike);
        SubscribeLocalEvent<CanPerformComboComponent, DragonAscensionPerformedEvent>(OnDragonAscension);

        SubscribeLocalEvent<GrantKungFuDragonComponent, UseInHandEvent>(OnGrantCQCUse);

        SubscribeLocalEvent<DragonPowerBuffComponent, AttackedEvent>(OnAttacked);

        SubscribeLocalEvent<StandingStateComponent, DownedEvent>(OnDownedSlide);
    }

    private const float SlideDistanceAtNormalSpeed = 3f;
    private const float SlideReferenceSpeed = 4.5f;

    private void OnAttacked(Entity<DragonPowerBuffComponent> ent, ref AttackedEvent args)
    {
        if (_hands.TryGetActiveItem(ent.Owner, out _) // Only unarmed
            || !_blocker.CanInteract(ent, null)) // Should be able to interact
            return;

        args.ModifiersList.Add(ent.Comp.ModifierSet);

        // Works for both armed and unarmed attacks
        ApplyMultiplier(ent,
            ent.Comp.DamageMultiplier,
            0f,
            ent.Comp.AttackDamageBuffDuration,
            MartialArtModifierType.Damage);
    }

    private void OnDownedSlide(EntityUid uid, StandingStateComponent component, DownedEvent args)
    {
        if (!TryComp<MartialArtsKnowledgeComponent>(uid, out var knowledge)
            || knowledge.MartialArtsForm != MartialArtsForms.KungFuDragon)
            return;

        if (!_combatMode.IsInCombatMode(uid))
            return;

        if (!TryComp<PhysicsComponent>(uid, out var physics) || _gravity.IsWeightless(uid))
            return;

        var speed = physics.LinearVelocity.Length();
        if (speed <= 0)
            return;

        var forward = _transform.GetWorldRotation(uid).ToWorldVec();
        var throwSpeed = SlideDistanceAtNormalSpeed * (speed / SlideReferenceSpeed);

        EnsureComp<GrabThrownComponent>(uid);
        _throwing.TryThrow(uid, forward, throwSpeed, animated: false, playSound: false, doSpin: false);
    }

    private void OnDragonStrike(Entity<CanPerformComboComponent> ent, ref DragonStrikePerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);

        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);
        _audio.PlayPvs(args.Sound, target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnDragonTail(Entity<CanPerformComboComponent> ent, ref DragonTailPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);

        if (downed)
            _stun.TryUpdateStunDuration(target, args.DownedParalyzeTime); // No stunlocks
        else
        {
            _stamina.TakeStaminaDamage(target, proto.StaminaDamage);
            _stun.TryKnockdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime), true, true, proto.DropItems);
            DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);
        }

        _audio.PlayPvs(args.Sound, target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }


    private void OnDragonClaw(Entity<CanPerformComboComponent> ent, ref DragonClawPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;
        _movementMod.TryUpdateMovementSpeedModDuration(target, MartsGenericSlow, args.SlowdownTime, args.WalkSpeedModifier, args.SprintSpeedModifier);
        _stamina.TakeStaminaDamage(target, proto.StaminaDamage);
        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);
        _audio.PlayPvs(args.Sound, target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnDragonAscension(Entity<CanPerformComboComponent> ent, ref DragonAscensionPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        if (!HasComp<DragonPowerBuffComponent>(ent))
        {
            _popupSystem.PopupEntity(Loc.GetString("martial-arts-fail-no-chi"), ent, ent);
            ent.Comp.LastAttacks.Clear();
            return;
        }

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage);
        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);

        var direction = _transform.GetWorldPosition(target) - _transform.GetWorldPosition(ent);
        if (direction != Vector2.Zero)
            _throwing.TryThrow(target, direction.Normalized() * args.ThrowDistance, args.ThrowSpeed, ent);

        if (args.Emote != null && TryComp(ent, out AnimatedEmotesComponent? emotes))
        {
            emotes.Emote = args.Emote.Value;
            Dirty(ent, emotes);
        }

        _audio.PlayPvs(args.Sound, target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }
}
