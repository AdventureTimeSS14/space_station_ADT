// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <aviu00@protonmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 JohnOakman <sremy2012@hotmail.fr>
// SPDX-FileCopyrightText: 2025 Lincoln McQueen <lincoln.mcqueen@gmail.com>
// SPDX-FileCopyrightText: 2025 Marcus F <marcus2008stoke@gmail.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
// SPDX-FileCopyrightText: 2025 Ted Lukin <66275205+pheenty@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 pheenty <fedorlukin2006@gmail.com>
// SPDX-FileCopyrightText: 2025 thebiggestbruh <199992874+thebiggestbruh@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 thebiggestbruh <marcus2008stoke@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Changeling.Components;
using Content.Shared.ADT.MartialArts;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Audio;
using Content.Shared.ADT.CustomFactionIcons;
using Content.Shared.Damage.Prototypes;

namespace Content.Shared.ADT.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializeSleepingCarp()
    {
        SubscribeLocalEvent<CanPerformComboComponent, SleepingCarpGnashingTeethPerformedEvent>(OnSleepingCarpGnashing);
        SubscribeLocalEvent<CanPerformComboComponent, SleepingCarpCrashingWavesPerformedEvent>(OnSleepingCarpCrashingWaves);
        SubscribeLocalEvent<CanPerformComboComponent, SleepingCarpDisarmCatchPerformedEvent>(OnSleepingCarpDisarmCatch);

        SubscribeLocalEvent<GrantSleepingCarpComponent, UseInHandEvent>(OnGrantSleepingCarp);
    }

    #region Generic Methods

    private void OnGrantSleepingCarp(Entity<GrantSleepingCarpComponent> ent, ref UseInHandEvent args)
    {
        if (!_netManager.IsServer)
            return;

        if (HasComp<ChangelingComponent>(args.User))
        {
            _popupSystem.PopupEntity(Loc.GetString("cqc-fail-changeling"), args.User, args.User);
            return;
        }

        if (!TryGrantMartialArt(args.User, ent.Comp))
            return;

        if (ent.Comp.LearnMessage != null)
            _popupSystem.PopupEntity(Loc.GetString(ent.Comp.LearnMessage), args.User, args.User);

        _faction.AddFaction(args.User, ent.Comp.FactionToAdd);
        var userFactionIcons = EnsureComp<CustomFactionIconsComponent>(args.User);
        userFactionIcons.FactionIcons.Add(ent.Comp.IconToAdd);
        var userReflect = EnsureComp<ReflectComponent>(args.User);
        userReflect.ReflectProb = 1;
        userReflect.Spread = 60;
        Dirty(args.User, userReflect);

        if (ent.Comp.MultiUse)
            return;

        QueueDel(ent);
        if (ent.Comp.SpawnedProto == null)
            return;

        var coords = Transform(ent).Coordinates;
        _audio.PlayPvs(ent.Comp.SoundOnUse, coords);
        Spawn(ent.Comp.SpawnedProto, coords);
    }

    #endregion

    #region Combo Methods

    private void OnSleepingCarpGnashing(Entity<CanPerformComboComponent> ent,
        ref SleepingCarpGnashingTeethPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !_proto.TryIndex<MartialArtPrototype>(proto.MartialArtsForm.ToString(), out var martialArtProto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        var bonusDamage = Math.Min(ent.Comp.ConsecutiveGnashes * 20, 80);
        var totalDamage = 10 + bonusDamage;

        DoDamage(ent, target, proto.DamageType, totalDamage, out _);
        ent.Comp.ConsecutiveGnashes++;
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg"), target);
        if (!downed)
        {
            var saying =
                Enumerable.ElementAt(martialArtProto.RandomSayings, _random.Next(martialArtProto.RandomSayings.Count));
            var ev = new SleepingCarpSaying(saying);
            RaiseLocalEvent(ent, ev);
        }
        else
        {
            var saying =
                Enumerable.ElementAt(martialArtProto.RandomSayingsDowned, _random.Next(martialArtProto.RandomSayingsDowned.Count));
            var ev = new SleepingCarpSaying(saying);
            RaiseLocalEvent(ent, ev);
        }
        ent.Comp.LastAttacks.Clear();
    }

    private void OnSleepingCarpCrashingWaves(Entity<CanPerformComboComponent> ent,
        ref SleepingCarpCrashingWavesPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _))
            return;

        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);
        var mapPos = _transform.GetMapCoordinates(ent).Position;
        var hitPos = _transform.GetMapCoordinates(target).Position;
        var dir = hitPos - mapPos;
        if (dir.LengthSquared() > 0)
            dir /= dir.Length();

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);
        _grabThrown.Throw(target, ent, dir, 25f, new DamageSpecifier(_proto.Index<DamageTypePrototype>("Blunt"), FixedPoint2.Zero));
        _newStatus.TryAddStatusEffectDuration(target, "StatusEffectForcedSleeping", out _, TimeSpan.FromSeconds(2));
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit2.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnSleepingCarpDisarmCatch(Entity<CanPerformComboComponent> ent,
        ref SleepingCarpDisarmCatchPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        DoDamage(ent, target, "Blunt", 10, out _);
        if (_hands.TryGetActiveItem(target, out var activeItem) && activeItem.HasValue)
        {
            if (_hands.TryGetEmptyHand(ent.Owner, out var emptyHand))
            {
                _hands.TryPickup(ent.Owner, activeItem.Value, emptyHand);
            }
        }

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    #endregion
}
