// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using System.Linq;
using Content.Shared.ADT.HolyDamage;
using Content.Shared.ADT.Vampire.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mindshield.Components;
using Content.Shared.Popups;

namespace Content.Server.ADT.Vampire;

public sealed partial class VampireSystem
{
    private void InitializeThralls()
    {
        SubscribeLocalEvent<ThrallOwnerComponent, DamageDealtEvent>(OnVampireDamageChanged);
        SubscribeLocalEvent<ThrallComponent, DamageDealtEvent>(OnThrallDamageChanged);
        SubscribeLocalEvent<MindShieldComponent, ComponentStartup>(MindShieldImplanted); // TODO: Replace this with a specific event
        SubscribeLocalEvent<ThrallComponent, ComponentShutdown>(OnThrallShutdown);
    }

    private void OnThrallShutdown(Entity<ThrallComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.CommuneActionEntity is { } action)
            _action.RemoveAction(ent.Owner, action);
    }

    #region Damage Sharing Logic

    private void OnVampireDamageChanged(EntityUid uid, ThrallOwnerComponent component, ref DamageDealtEvent args)
    {
        if (args.Damage.GetTotal() <= 0)
            return;

        if (!TryComp<ThrallOwnerComponent>(uid, out var thrallOwner) || !thrallOwner.DamageSharing)
            return;

        var aliveThralls = GetAliveThralls(thrallOwner);
        if (aliveThralls.Count == 0)
            return;

        _damage.TryChangeDamage(uid, -args.Damage, true, false);
        DistributeDamage(uid, args.Damage, aliveThralls);
    }

    private void OnThrallDamageChanged(EntityUid uid, ThrallComponent component, ref DamageDealtEvent args)
    {
        if (args.Damage.GetTotal() <= 0)
            return;

        if (component.VampireOwner is not { } vampireOwner)
            return;

        if (!TryComp<ThrallOwnerComponent>(vampireOwner, out var thrallOwner) || !thrallOwner.DamageSharing)
            return;

        var aliveThralls = GetAliveThralls(thrallOwner);
        if (aliveThralls.Count == 0)
            return;

        _damage.TryChangeDamage(uid, -args.Damage, true, false);
        DistributeDamage(vampireOwner, args.Damage, aliveThralls);
    }

    private void DistributeDamage(EntityUid vampireUid, DamageSpecifier originalDamage, List<EntityUid> aliveThralls)
    {
        var participants = new List<EntityUid> { vampireUid };
        participants.AddRange(aliveThralls);

        if (participants.Count == 0)
            return;

        var sharedDamage = GetSharedDamage(originalDamage, participants.Count);
        foreach (var participant in participants)
        {
            if (!HasComp<DamageableComponent>(participant))
                continue;

            _damage.TryChangeDamage(participant, sharedDamage, true, false);
        }
    }

    #endregion

    private void MindShieldImplanted(EntityUid uid, MindShieldComponent comp, ComponentStartup init)
    {
        if (!TryComp<ThrallComponent>(uid, out var thrall) || thrall.VampireOwner is not { } owner)
            return;

        var stunTime = TimeSpan.FromSeconds(4);
        var name = Identity.Entity(uid, EntityManager);

        if (TryComp<ThrallOwnerComponent>(owner, out var thrallOwner))
        {
            TryRemoveThrall(thrallOwner, uid);
            Dirty(owner, thrallOwner);
        }

        RemComp<ThrallComponent>(uid);
        RemComp<HolyAffectedComponent>(uid);
        RemComp<HolySuppressionComponent>(uid);
        _stun.TryUpdateParalyzeDuration(uid, stunTime);
        _popup.PopupEntity(Loc.GetString("thrall-break-control", ("name", name)), uid);
    }

    private void UpdateThrallCount(Entity<VampireComponent?, ThrallOwnerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, false) || !Resolve(ent, ref ent.Comp2, false))
            return;

        var blood = ent.Comp1.CurrentBlood.Float();

        var sortedThresholds = ent.Comp2.ThrallCountThresholds.Keys.OrderBy(x => x).ToList();
        foreach (var threshold in sortedThresholds)
        {
            if (blood >= threshold && !ent.Comp2.UnlockedThresholds.Contains(threshold))
                ent.Comp2.UnlockedThresholds.Add(threshold);
        }

        var totalBonus = ent.Comp2.UnlockedThresholds.Sum(t => ent.Comp2.ThrallCountThresholds[t]);

        var newMaxCount = 1 + totalBonus;
        if (ent.Comp2.MaxThrallCount != newMaxCount)
        {
            ent.Comp2.MaxThrallCount = newMaxCount;
            Dirty(ent.Owner, ent.Comp2);

            _popup.PopupEntity(Loc.GetString("vampire-trall-count-update", ("count", newMaxCount)), ent, ent, PopupType.Medium);
        }
    }
}
