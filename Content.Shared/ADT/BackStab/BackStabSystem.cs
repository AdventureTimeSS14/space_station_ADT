// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aviu00 <93730715+Aviu00@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Spatison <137375981+Spatison@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.ADT.BackStab;

public sealed class BackStabSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    public static readonly Angle DefaultTolerance = Angle.FromDegrees(45d);

    public static readonly SoundSpecifier BackstabSound =
        new SoundPathSpecifier("/Audio/_Goobstation/Weapons/Effects/guillotine.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BackStabComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<BackStabComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        foreach (var target in args.HitEntities)
        {
            if (target == args.User)
                continue;

            if (!IsBehindTarget(args.User, target, ent.Comp.Tolerance))
                continue;

            _damageable.TryChangeDamage(target, ent.Comp.BonusDamage, origin: args.User);
        }
    }

    public bool IsBehindTarget(EntityUid user, EntityUid target, Angle? tolerance = null)
    {
        var behindAngle = _transform.GetWorldRotation(target).GetCardinalDir().GetOpposite().ToAngle();

        var userPos = _transform.GetMapCoordinates(user).Position;
        var targetPos = _transform.GetMapCoordinates(target).Position;
        var currentAngle = (userPos - targetPos).ToWorldAngle();

        var difference = Angle.ShortestDistance(behindAngle, currentAngle);
        var maxDifference = tolerance ?? DefaultTolerance;

        return Math.Abs(difference.Degrees) < maxDifference.Degrees;
    }

    public bool TryBackstab(EntityUid target,
        EntityUid user,
        Angle? tolerance = null,
        bool showPopup = true,
        bool playSound = true,
        bool alwaysBackstabLaying = true)
    {
        if (target == user || !HasComp<MobStateComponent>(target))
            return false;

        if (!(alwaysBackstabLaying && _standing.IsDown(target)) && !IsBehindTarget(user, target, tolerance))
            return false;

        BackstabEffects(target, showPopup, playSound);
        return true;
    }

    private void BackstabEffects(EntityUid target, bool showPopup, bool playSound)
    {
        if (_net.IsClient)
            return;

        if (showPopup)
            _popup.PopupEntity(Loc.GetString("backstab-message"), target, PopupType.LargeCaution);

        if (playSound)
            _audio.PlayPvs(BackstabSound, target);
    }
}
