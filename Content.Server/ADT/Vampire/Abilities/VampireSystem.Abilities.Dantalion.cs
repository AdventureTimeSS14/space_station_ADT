// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using System.Linq;
using Content.Server.Administration;
using Content.Server.Cloning;
using Content.Server.ADT.Hallucinations.Systems;
using Content.Server.Prayer;
using Content.Shared.Bible.Components;
using Content.Shared.Body;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects.Effects.StatusEffects;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Stealth.Components;
using Content.Shared.Stunnable;
using Content.Shared.ADT.Vampire.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Shared.ADT.Vampire;

namespace Content.Server.ADT.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private CloningSystem _cloning = default!;
    [Dependency] private SchizophreniaSystem _hallucinations = default!;
    [Dependency] private PrayerSystem _prayerSystem = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;

    private void InitializeDantalion()
    {
        SubscribeLocalEvent<VampireComponent, VampireEnthrallActionEvent>(OnAfterEnthrall);
        SubscribeLocalEvent<VampireComponent, EnthrallDoAfterEvent>(OnEnthrallDoAfter);
        SubscribeLocalEvent<VampireComponent, VampireCommuneActionEvent>(OnCommune);
        SubscribeLocalEvent<VampireComponent, VampirePacifyActionEvent>(OnPacify);
        SubscribeLocalEvent<VampireComponent, VampireSubspaceSwapActionEvent>(OnSubspaceSwap);
        SubscribeLocalEvent<VampireComponent, VampireDeployDecoyActionEvent>(OnDeployDecoy);
        SubscribeLocalEvent<VampireComponent, VampireRallyThrallsActionEvent>(OnRallyThralls);
        SubscribeLocalEvent<VampireComponent, VampireBloodBondActionEvent>(OnBloodBond);
        SubscribeLocalEvent<VampireComponent, VampireMassHysteriaActionEvent>(OnMassHysteria);
        SubscribeLocalEvent<VampireComponent, VampireThrallHealActionEvent>(OnThrallHeal);
        SubscribeLocalEvent<VampireComponent, VampirePacifyNearbyActionEvent>(OnPacifyNearby);
        SubscribeLocalEvent<ThrallComponent, VampireThrallCommuneActionEvent>(OnThrallCommune);
    }

    private void OnAfterEnthrall(Entity<VampireComponent> ent, ref VampireEnthrallActionEvent args)
    {
        var target = args.Target;
        if (!TryComp<ThrallOwnerComponent>(ent, out var thrallOwner))
            return;

        if (!CanAddThrall(thrallOwner))
        {
            _popup.PopupEntity(Loc.GetString("vampire-max-trall-reached"), ent, ent, PopupType.Medium);
            return;
        }

        if (HasComp<VampireComponent>(target) || HasComp<MindShieldComponent>(target) || HasComp<BibleUserComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-enthall-failed", ("target", Identity.Name(target, EntityManager))), ent, ent);
            return;
        }

        if (TryComp<ThrallComponent>(target, out var trallComponent))
        {
            if (trallComponent.VampireOwner == ent.Owner)
            {
                _popup.PopupEntity(Loc.GetString("vampire-enthall-already", ("target", Identity.Name(target, EntityManager))), ent, ent);
                return;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("vampire-enthall-failed", ("target", Identity.Name(target, EntityManager))), ent, ent);
                return;
            }
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("vampire-blooddrink-countion"), ent, args.Target, PopupType.MediumCaution);
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, TimeSpan.FromSeconds(15f), new EnthrallDoAfterEvent(args.BloodCost), ent, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 0.5f,
            NeedHand = true
        });
    }

    private void OnEnthrallDoAfter(Entity<VampireComponent> ent, ref EnthrallDoAfterEvent args)
    {
        if (args.Cancelled || args.Args.Target == null) return;

        var target = args.Args.Target.Value;
        if (!HasComp<ActorComponent>(target))
            return;

        if (!TryComp<ThrallOwnerComponent>(ent, out var thrallOwner))
            return;

        MakeUnholy(target);
        var newTrall = EnsureComp<ThrallComponent>(target);
        newTrall.VampireOwner = ent.Owner;
        newTrall.CommuneActionEntity = _action.AddAction(target, "ActionThrallCommune");
        Dirty(target, newTrall);

        if (TryAddThrall(thrallOwner, target))
            Dirty(ent.Owner, thrallOwner);

        _popup.PopupEntity(Loc.GetString("vampire-enthall-success", ("target", Identity.Name(target, EntityManager))), ent, ent);
        _antag.SendBriefing(target, Loc.GetString("thrall-greeting"), Color.Red,
            new SoundPathSpecifier("/Audio/ADT/Vampire/Ambience/Antag/vampare_start.ogg"));
        SubtractBloodEssence(ent.Owner, args.BloodCost);
    }

    private void OnCommune(Entity<VampireComponent> ent, ref VampireCommuneActionEvent args)
    {
        if (!TryComp<ThrallOwnerComponent>(ent, out var thrallOwner) || thrallOwner.ThrallOwned.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-no-thrall"), ent, ent, PopupType.Medium);
            return;
        }

        if (!TryComp<ActorComponent>(ent, out var playerActor))
            return;

        var playerSession = playerActor.PlayerSession;
        _quickDialog.OpenDialog(playerSession, Loc.GetString("vampire-commune-title"), Loc.GetString("vampire-commune-prompt"),
            (string message) =>
            {
                var finalMessage = string.IsNullOrWhiteSpace(message)
                    ? Loc.GetString("vampire-commune-default-message")
                    : FormattedMessage.EscapeText(message);

                foreach (var thrallUid in thrallOwner.ThrallOwned)
                {
                    if (!TryComp<ActorComponent>(thrallUid, out var thrallActor))
                        continue;

                    _prayerSystem.SendSubtleMessage(thrallActor.PlayerSession, thrallActor.PlayerSession, finalMessage, Loc.GetString("vampire-commune-default-message"));
                    _chat.SendMessageToOne(thrallUid, finalMessage);
                }

                _chat.SendMessageToOne(ent, finalMessage);
            });

        args.Handled = true;
    }

    private void OnThrallCommune(Entity<ThrallComponent> ent, ref VampireThrallCommuneActionEvent args)
    {
        if (ent.Comp.VampireOwner is not { } owner || !TryComp<ThrallOwnerComponent>(owner, out var thrallOwner))
            return;

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        _quickDialog.OpenDialog(actor.PlayerSession, Loc.GetString("vampire-thrall-commune-title"), Loc.GetString("vampire-thrall-commune-prompt"),
            (string message) =>
            {
                var finalMessage = string.IsNullOrWhiteSpace(message)
                    ? Loc.GetString("vampire-commune-default-message")
                    : FormattedMessage.EscapeText(message);

                var formatted = Loc.GetString("vampire-thrall-commune-message",
                    ("name", Identity.Name(ent, EntityManager)), ("message", finalMessage));

                if (Exists(owner))
                    _chat.SendMessageToOne(owner, formatted);

                foreach (var thrallUid in thrallOwner.ThrallOwned)
                {
                    if (thrallUid == ent.Owner || !Exists(thrallUid))
                        continue;

                    _chat.SendMessageToOne(thrallUid, formatted);
                }

                _chat.SendMessageToOne(ent, finalMessage);
            });

        args.Handled = true;
    }

    private void OnPacify(Entity<VampireComponent> ent, ref VampirePacifyActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var target = args.Target;
        if (HasComp<HumanoidProfileComponent>(target))
        {
            if (IsProtectedByFaith(target, ent) || HasComp<ThrallComponent>(target))
            {
                _popup.PopupEntity(Loc.GetString("vampire-pacify-failed", ("target", Identity.Name(target, EntityManager))), ent, ent);
                return;
            }

            if (!_mobState.IsDead(target) && !HasComp<PacifiedComponent>(target))
            {
                EnsureComp<PacifiedComponent>(target);
                Timer.Spawn(args.Time, () => { RemComp<PacifiedComponent>(target); });

                SubtractBloodEssence(ent.Owner, args.BloodCost);
                args.Handled = true;
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("vampire-pacify-failed", ("target", Identity.Name(target, EntityManager))), ent, ent);
                args.Handled = true;
            }
        }
    }

    private void OnSubspaceSwap(Entity<VampireComponent> ent, ref VampireSubspaceSwapActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var target = args.Target;
        if (!HasComp<HumanoidProfileComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-teleport-failed"), ent, ent, PopupType.SmallCaution);
            return;
        }

        if (IsProtectedByFaith(target, ent))
            return;

        var currentCoords = Transform(ent).Coordinates;
        var targetCoords = Transform(target).Coordinates;
        _transform.SetCoordinates(ent, targetCoords);
        _transform.SetCoordinates(target, currentCoords);

        _movementMod.TryUpdateMovementSpeedModDuration(target, SharedStunSystem.SlowdownEffect, TimeSpan.FromSeconds(4f), 0.5f);
        _hallucinations.AddOrAdjustHallucinations(target, "Vampire", 15f, StatusEffectMetabolismType.Set);

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnDeployDecoy(Entity<VampireComponent> ent, ref VampireDeployDecoyActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        if (!_cloning.TryCloning(ent, _transform.GetMapCoordinates(ent), args.Settings, out var clone))
            return;

        EntityManager.AddComponents(clone.Value, args.EnsurableComponents);

        var stealth = EnsureComp<StealthComponent>(ent);
        _stealth.SetVisibility(ent, 0f, stealth);

        Timer.Spawn(args.Time, () =>
        {
            RemComp<StealthComponent>(ent);
            Del(clone);
        });

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnRallyThralls(Entity<VampireComponent> ent, ref VampireRallyThrallsActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var thrallsInRange = _entityLookup.GetEntitiesInRange<ThrallComponent>(Transform(ent).Coordinates, 7f);
        foreach (var thrallEntity in thrallsInRange)
        {
            if (_mobState.IsDead(thrallEntity.Owner))
                continue;

            TryRemoveKnockdown(thrallEntity.Owner);
            _stamina.RemoveStaminaDamage(thrallEntity.Owner);
            _status.TryRemoveStatusEffect(thrallEntity.Owner, ForceSleeping);
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnBloodBond(Entity<VampireComponent> ent, ref VampireBloodBondActionEvent args)
    {
        var supreme = GetTruePower(ent);
        if (supreme == null)
            return;

        if (!TryComp<ThrallOwnerComponent>(ent, out var thrallOwner) || thrallOwner.ThrallOwned.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("vampire-no-thrall"), ent, ent, PopupType.Medium);
            return;
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        if (supreme.Active)
        {
            supreme.Active = false;
            thrallOwner.DamageSharing = false;
            Dirty(ent.Owner, supreme);
            args.Handled = true;
            return;
        }

        supreme.Active = true;
        thrallOwner.DamageSharing = true;
        Dirty(ent.Owner, supreme);

        ExecuteBloodBondTick(ent, supreme, thrallOwner, args);
        args.Handled = true;
    }

    private void OnThrallHeal(Entity<VampireComponent> ent, ref VampireThrallHealActionEvent args)
    {
        var thrallsInRange = _entityLookup.GetEntitiesInRange<ThrallComponent>(Transform(ent).Coordinates, 5);

        int thrallCount = thrallsInRange.Count;
        if (thrallCount == 0)
            return;

        if (!CheckBloodEssence(ent.Owner, thrallCount * args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        foreach (var thrallEntity in thrallsInRange)
        {
            ExecuteThrallHealTick(thrallEntity.Owner, ent, 0, args);
        }

        SubtractBloodEssence(ent.Owner, thrallCount * args.BloodCost);
        args.Handled = true;
    }

    private void OnPacifyNearby(Entity<VampireComponent> ent, ref VampirePacifyNearbyActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var peopleInRange = _entityLookup.GetEntitiesInRange<HumanoidProfileComponent>(Transform(ent).Coordinates, 6);
        foreach (var person in peopleInRange)
        {
            if (HasComp<ThrallComponent>(person) || HasComp<VampireComponent>(person))
                continue;

            if (IsProtectedByFaith(person, ent))
                continue;

            if (_mobState.IsDead(person))
                continue;

            if (HasComp<PacifiedComponent>(person))
                continue;

            var target = person;
            EnsureComp<PacifiedComponent>(target);
            Timer.Spawn(args.Time, () => RemComp<PacifiedComponent>(target));
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    private void OnMassHysteria(Entity<VampireComponent> ent, ref VampireMassHysteriaActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var victimInRange = _entityLookup.GetEntitiesInRange<BodyComponent>(Transform(ent).Coordinates, 8f)
            .Where(entity => entity.Owner != ent.Owner).ToList();

        foreach (var victimEntity in victimInRange)
        {
            if (IsProtectedByFaith(victimEntity, ent))
                continue;

            _flash.Flash(victimEntity, ent, null, TimeSpan.FromSeconds(4f), 0.5f);
            _hallucinations.AddOrAdjustHallucinations(victimEntity, "VampireUlt", 30f, StatusEffectMetabolismType.Set);
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }

    #region Utility Methods

    private void ExecuteBloodBondTick(Entity<VampireComponent> ent, SupremeVampireComponent supreme, ThrallOwnerComponent thrallOwner, VampireBloodBondActionEvent args)
    {
        if (!Exists(ent) || !supreme.Active)
        {
            supreme.Active = false;
            thrallOwner.DamageSharing = false;
            Dirty(ent.Owner, supreme);
            return;
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);

            supreme.Active = false;
            thrallOwner.DamageSharing = false;
            Dirty(ent.Owner, supreme);
            return;
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);

        Timer.Spawn(args.TimeInterval, () => ExecuteBloodBondTick(ent, supreme, thrallOwner, args));
    }

    private void ExecuteThrallHealTick(EntityUid thrallUid, Entity<VampireComponent> vampire, int currentTick, VampireThrallHealActionEvent args)
    {
        if (!Exists(thrallUid) || !Exists(vampire) || currentTick >= args.Repeats)
            return;

        var healingSpec = CalculateScaledHealing(thrallUid, args.Heal, args.HealGroups);
        _damage.TryChangeDamage(thrallUid, healingSpec, true, false, origin: vampire);

        Timer.Spawn(args.TimeInterval, () => ExecuteThrallHealTick(thrallUid, vampire, currentTick + 1, args));
    }

    #endregion
}
