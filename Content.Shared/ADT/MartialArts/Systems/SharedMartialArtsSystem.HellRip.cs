// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 August Eymann <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 Bokser815 <70928915+Bokser815@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 Lincoln McQueen <lincoln.mcqueen@gmail.com>
// SPDX-FileCopyrightText: 2025 Lumminal <81829924+Lumminal@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.ADT.MartialArts;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;

using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;

namespace Content.Shared.ADT.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializeHellRip()
    {
        SubscribeLocalEvent<CanPerformComboComponent, HellRipSlamPerformedEvent>(OnHellRipSlam);
        SubscribeLocalEvent<CanPerformComboComponent, HellRipDropKickPerformedEvent>(OnHellRipDropKick);
        SubscribeLocalEvent<CanPerformComboComponent, HellRipHeadRipPerformedEvent>(OnHellRipHeadRip);
        SubscribeLocalEvent<CanPerformComboComponent, HellRipTearDownPerformedEvent>(OnHellRipTearDown);

        SubscribeLocalEvent<GrantHellRipComponent, MapInitEvent>(OnGrantHellRip);
        SubscribeLocalEvent<GrantHellRipComponent, ComponentShutdown>(OnRemoveHellRip);
        SubscribeLocalEvent<GrantHellRipComponent, UseInHandEvent>(OnGrantCQCUse);
    }

    #region Generic Methods

    private void OnGrantHellRip(Entity<GrantHellRipComponent> ent, ref MapInitEvent args)
    {
        if (!HasComp<MobStateComponent>(ent))
            return;

        TryGrantMartialArt(ent.Owner, ent.Comp);
    }


    private void OnRemoveHellRip(Entity<GrantHellRipComponent> ent, ref ComponentShutdown args)
    {
        var user = ent.Owner;

        RemComp<MartialArtsKnowledgeComponent>(user);
        RemComp<CanPerformComboComponent>(user);
    }

    #endregion

    #region Combo Methods

    private void OnHellRipHeadRip(Entity<CanPerformComboComponent> ent, ref HellRipHeadRipPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        if (!_mobState.IsDead(target))
        {
            _popupSystem.PopupEntity(Loc.GetString("martial-arts-fail-target-alive"), ent, ent);
            ent.Comp.LastAttacks.Clear();
            return;
        }

        if (!TryComp<PullableComponent>(target, out var pullable))
            return;

        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);

        _pulling.TryStopPull(target, pullable, ent, true);

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", 300);
        _damageable.TryChangeDamage(target, damage, true, origin: ent);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/gib1.ogg"), target);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/demon_attack1.ogg"), ent);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnHellRipSlam(Entity<CanPerformComboComponent> ent, ref HellRipSlamPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed)
            || downed)
            return;

        if (!TryComp<StandingStateComponent>(ent, out var userStanding) || userStanding.Standing)
        {
            _popupSystem.PopupEntity(Loc.GetString("martial-arts-fail-not-prone"), ent, ent);
            ent.Comp.LastAttacks.Clear();
            return;
        }

        var knockdownTime = TimeSpan.FromSeconds(proto.ParalyzeTime);

        var ev = new BeforeStaminaDamageEvent(1f);
        RaiseLocalEvent(target, ref ev);

        knockdownTime *= ev.Value;

        _stun.TryKnockdown(target, knockdownTime, true, true, proto.DropItems);

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage);

        //_pulling.TryStopPull(target, pullable, ent, true);

        if (HasComp<KnockedDownComponent>(ent.Owner))
            RemComp<KnockedDownComponent>(ent.Owner);
        _standingState.Stand(ent);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/demon_attack1.ogg"), ent);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/metal_slam5.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnHellRipDropKick(Entity<CanPerformComboComponent> ent, ref HellRipDropKickPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        if (!TryComp<PullableComponent>(ent, out var selfPullable) || selfPullable.Puller == null)
        {
            _popupSystem.PopupEntity(Loc.GetString("martial-arts-fail-not-grabbed"), ent, ent);
            ent.Comp.LastAttacks.Clear();
            return;
        }

        var puller = selfPullable.Puller.Value;
        _pulling.TryStopPull(ent, selfPullable, puller, true);

        var entPos = _transform.GetMapCoordinates(ent).Position;
        var pullerPos = _transform.GetMapCoordinates(puller).Position;
        var direction = pullerPos - entPos;
        _grabThrown.Throw(puller, ent, direction, 25, behavior: proto.DropItems);

        _stamina.TakeStaminaDamage(ent, -60f, ignoreResist: true);
        if (HasComp<KnockedDownComponent>(ent.Owner))
            RemComp<KnockedDownComponent>(ent.Owner);
        _standingState.Stand(ent);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/demon_attack1.ogg"), ent);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnHellRipTearDown(Entity<CanPerformComboComponent> ent,
       ref HellRipTearDownPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !_proto.TryIndex<MartialArtPrototype>(proto.MartialArtsForm.ToString(), out var martialArtProto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed)
            || !TryComp<PullableComponent>(target, out var pullable))
            return;

        _pulling.TryStopPull(target, pullable, ent, true);

        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);
        _stamina.TakeStaminaDamage(target, proto.StaminaDamage, source: ent);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/Fluids/blood1.ogg"), target);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/demon_attack1.ogg"), ent);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();

    }
    #endregion
}
