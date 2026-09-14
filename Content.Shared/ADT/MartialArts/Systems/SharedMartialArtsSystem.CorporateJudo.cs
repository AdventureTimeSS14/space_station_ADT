// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 August Eymann <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Lincoln McQueen <lincoln.mcqueen@gmail.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.MartialArts;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Flash;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializeCorporateJudo()
    {
        SubscribeLocalEvent<CanPerformComboComponent, JudoLegSweepPerformedEvent>(OnJudoLegSweep);
        SubscribeLocalEvent<CanPerformComboComponent, JudoCombatGrabPerformedEvent>(OnJudoCombatGrab);
        SubscribeLocalEvent<CanPerformComboComponent, JudoEyeGougePerformedEvent>(OnJudoEyeGouge);
        SubscribeLocalEvent<CanPerformComboComponent, JudoNageWazaPerformedEvent>(OnJudoNageWaza);

        SubscribeLocalEvent<GrantCorporateJudoComponent, ClothingGotEquippedEvent>(OnGrantCorporateJudo);
        SubscribeLocalEvent<GrantCorporateJudoComponent, ClothingGotUnequippedEvent>(OnRemoveCorporateJudo);

        SubscribeLocalEvent<StunbatonComponent, AttemptMeleeEvent>(OnStunbatonMeleeAttempt);
    }

    private void OnStunbatonMeleeAttempt(Entity<StunbatonComponent> ent, ref AttemptMeleeEvent args)
    {
        if (args.Cancelled
            || !TryComp<MartialArtsKnowledgeComponent>(args.User, out var knowledge)
            || knowledge.MartialArtsForm != MartialArtsForms.CorporateJudo)
            return;

        args.Cancelled = true;
        args.Message = Loc.GetString("judo-fail-stunbaton");
    }

    private void OnGrantCorporateJudo(Entity<GrantCorporateJudoComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!_netManager.IsServer)
            return;

        var user = args.Wearer;
        TryGrantMartialArt(user, ent.Comp);
    }

    private void OnRemoveCorporateJudo(Entity<GrantCorporateJudoComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        var user = args.Wearer;
        if (!TryComp<MartialArtsKnowledgeComponent>(user, out var martialArtsKnowledge))
            return;

        if (martialArtsKnowledge.MartialArtsForm != MartialArtsForms.CorporateJudo)
            return;

        if (!TryComp<MeleeWeaponComponent>(args.Wearer, out var meleeWeaponComponent))
            return;

        var originalDamage = new DamageSpecifier();
        originalDamage.DamageDict[martialArtsKnowledge.OriginalFistDamageType]
            = FixedPoint2.New(martialArtsKnowledge.OriginalFistDamage);
        meleeWeaponComponent.Damage = originalDamage;

        RemComp<MartialArtsKnowledgeComponent>(user);
        RemComp<CanPerformComboComponent>(user);
    }

    private void OnJudoLegSweep(Entity<CanPerformComboComponent> ent, ref JudoLegSweepPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        if (downed)
        {
            _popupSystem.PopupEntity(Loc.GetString("martial-arts-fail-target-down"), ent, ent);
            ent.Comp.LastAttacks.Clear();
            return;
        }

        var knockdownTime = TimeSpan.FromSeconds(proto.ParalyzeTime);

        var ev = new BeforeStaminaDamageEvent(1f);
        RaiseLocalEvent(target, ref ev);

        knockdownTime *= ev.Value;

        _stun.TryKnockdown(target, knockdownTime, true, true, proto.DropItems);
        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnJudoCombatGrab(Entity<CanPerformComboComponent> ent, ref JudoCombatGrabPerformedEvent args)
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

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage);
        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnJudoEyeGouge(Entity<CanPerformComboComponent> ent, ref JudoEyeGougePerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _)
            || !TryComp(target, out StatusEffectsComponent? status))
            return;

        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);

        var flashAttempt = new FlashAttemptEvent(target, ent.Owner, null);
        RaiseLocalEvent(target, ref flashAttempt, true);

        if (!flashAttempt.Cancelled)
        {
            _status.TryAddStatusEffect<BlurryVisionComponent>(target,
                "BlurryVision",
                TimeSpan.FromSeconds(args.BlindDuration),
                true,
                status);

            _blindable.AdjustEyeDamage((target, null), args.EyeDamageAmount);
        }

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnJudoNageWaza(Entity<CanPerformComboComponent> ent, ref JudoNageWazaPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        if (!TryComp<PullerComponent>(target, out var targetPuller)
            || targetPuller.Pulling != ent.Owner)
        {
            _popupSystem.PopupEntity(Loc.GetString("martial-arts-fail-not-grabbed"), ent, ent);
            ent.Comp.LastAttacks.Clear();
            return;
        }

        if (TryComp<PullableComponent>(ent, out var selfPullable))
            _pulling.TryStopPull(ent, selfPullable, target, true);

        var mapPos = _transform.GetMapCoordinates(ent).Position;
        var hitPos = _transform.GetMapCoordinates(target).Position;
        var dir = hitPos - mapPos;

        if (TryComp<PullableComponent>(target, out var targetPullable))
            _pulling.TryStopPull(target, targetPullable, ent, true);

        _grabThrown.Throw(target, ent, dir, proto.ThrownSpeed, behavior: proto.DropItems);
        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }
}
