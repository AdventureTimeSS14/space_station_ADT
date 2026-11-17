// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.ADT.Vampire.Components;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Vampire;

public abstract partial class SharedVampireSystem : EntitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SharedPopupSystem _sharedPopup = default!;

    private static readonly TimeSpan GunPopupDelay = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VampireComponent, ShotAttemptedEvent>(OnShotAttempted);
    }

    private void OnShotAttempted(Entity<VampireComponent> ent, ref ShotAttemptedEvent args)
    {
        if (ent.Comp.CurrentEvolution is not (VampireClassEnum.Hemomancer or VampireClassEnum.Gargantua))
            return;

        args.Cancel();

        var now = _gameTiming.CurTime;
        if (now < ent.Comp.NextGunPopup)
            return;

        ent.Comp.NextGunPopup = now + GunPopupDelay;
        _sharedPopup.PopupClient(Loc.GetString("gun-disabled"), ent, ent, PopupType.LargeCaution);
    }

    #region Blood Management

    public bool CheckBloodEssence(VampireComponent component, FixedPoint2 requiredAmount, FixedPoint2? nullDamage = null)
    {
        var adjustedQuantity = requiredAmount * (1 + (nullDamage?.Float() ?? 0) / 100);
        return component.CurrentBlood >= adjustedQuantity;
    }

    public bool TryAddBlood(VampireComponent component, FixedPoint2 quantity, out FixedPoint2 newAmount)
    {
        newAmount = component.CurrentBlood + quantity;
        if (quantity <= 0)
            return false;

        component.CurrentBlood = newAmount;
        component.TotalBloodDrank += (float)quantity;
        return true;
    }

    public bool TrySubtractBlood(VampireComponent component, FixedPoint2 quantity, FixedPoint2? nullDamage = null)
    {
        var adjustedQuantity = quantity * (1 + (nullDamage?.Float() ?? 0) / 100);
        if (adjustedQuantity <= 0 || component.CurrentBlood < adjustedQuantity)
            return false;

        component.CurrentBlood -= adjustedQuantity;
        return true;
    }

    #endregion

    #region Skill Management

    public bool TryGetThresholdsForClass(VampireComponent component, [NotNullWhen(true)] out Dictionary<float, List<EntProtoId>>? thresholds)
    {
        thresholds = null;

        if (component.Variant != VampireVariant.Classic)
            return component.VariantThresholds.TryGetValue(component.Variant, out thresholds);

        if (component.CurrentEvolution == VampireClassEnum.NonSelected)
            return false;

        return component.ClassThresholds.TryGetValue(component.CurrentEvolution, out thresholds);
    }

    public bool HasSkill(VampireComponent component, EntProtoId skill)
    {
        return component.AcquiredSkills.ContainsKey(skill);
    }

    public List<EntProtoId> GetNewSkillsToAdd(VampireComponent component)
    {
        var newSkills = new List<EntProtoId>();
        if (component.CurrentEvolution == VampireClassEnum.NonSelected && component.Variant == VampireVariant.Classic)
            return newSkills;

        if (!TryGetThresholdsForClass(component, out var thresholds))
            return newSkills;

        foreach (var (threshold, skills) in thresholds.OrderBy(kv => kv.Key))
        {
            if (component.CurrentBlood >= threshold)
            {
                foreach (var skill in skills)
                {
                    if (!HasSkill(component, skill))
                        newSkills.Add(skill);
                }
            }
        }

        return newSkills;
    }

    #endregion

    #region Thrall Management

    public bool CanAddThrall(ThrallOwnerComponent owner)
    {
        return owner.ThrallOwned.Count < owner.MaxThrallCount;
    }

    public bool TryAddThrall(ThrallOwnerComponent owner, EntityUid thrallUid)
    {
        if (!CanAddThrall(owner))
            return false;

        if (!owner.ThrallOwned.Contains(thrallUid))
        {
            owner.ThrallOwned.Add(thrallUid);
            return true;
        }

        return false;
    }

    public bool TryRemoveThrall(ThrallOwnerComponent owner, EntityUid thrallUid)
    {
        if (owner.ThrallOwned.Remove(thrallUid))
            return true;

        return false;
    }

    public List<EntityUid> GetAliveThralls(ThrallOwnerComponent owner)
    {
        return owner.ThrallOwned.Where(Exists)
            .Where(t => !_mobState.IsDead(t))
            .ToList();
    }

    #endregion

    #region Bestia Management

    public void RecordExtraction(EntityUid vampire, EntityUid victim, [NotNullWhen(true)] out BestiaContainerComponent? bestia)
    {
        if (!TryComp(vampire, out bestia))
            return;

        if (!bestia.OrgansExtractedFromVictim.TryAdd(victim, 1))
            bestia.OrgansExtractedFromVictim[victim]++;
    }

    #endregion

    #region True Power Management

    public bool ShouldHaveTruePower(VampireComponent component)
    {
        if (component.Variant != VampireVariant.Classic)
            return component.CurrentBlood >= 500;

        return component.CurrentBlood >= 1000 && component.DrainedVictims.Count >= 8;
    }

    public bool HasTruePower(EntityUid uid)
    {
        return HasComp<SupremeVampireComponent>(uid);
    }

    public SupremeVampireComponent? GetTruePower(EntityUid uid)
    {
        return CompOrNull<SupremeVampireComponent>(uid);
    }

    #endregion

    #region Night Vision

    public Color GetNightVisionColorForClass(VampireClassEnum vampireClass)
    {
        return vampireClass switch
        {
            VampireClassEnum.Umbrae => Color.FromHex("#663ca3"),
            _ => Color.FromHex("#adadad")
        };
    }

    public EntProtoId GetNightVisionEffectForClass(VampireClassEnum vampireClass, bool hasTruePower = false)
    {
        if (hasTruePower)
            return "ADTEffectNightVisionVampireSupreme";

        return vampireClass switch
        {
            VampireClassEnum.Umbrae => "ADTEffectNightVisionVampireStrong",
            VampireClassEnum.Ancient => "ADTEffectNightVisionVampireStrong",
            _ => "ADTEffectNightVisionVampire"
        };
    }

    #endregion

    #region Eye Color Management

    public void SetEyeColor(EntityUid uid, Color color)
    {
        if (_visualBody.TryGatherMarkingsData(uid, null, out var profiles, out _, out _))
        {
            var newProfiles = profiles.ToDictionary(
                kv => kv.Key,
                kv => kv.Value with { EyeColor = color }
            );
            _visualBody.ApplyProfiles(uid, newProfiles);
        }
    }

    public Color GetCurrentEyeColor(EntityUid uid)
    {
        if (_visualBody.TryGatherMarkingsData(uid, null, out var profiles, out _, out _))
        {
            var firstProfile = profiles.Values.FirstOrDefault();
            return firstProfile.EyeColor;
        }

        return Color.White;
    }

    public Color GetVampireEyeColor(VampireClassEnum vampireClass)
    {
        return vampireClass switch
        {
            VampireClassEnum.Hemomancer => Color.FromHex("#eb251b"),
            VampireClassEnum.Umbrae => Color.FromHex("#b8188a"),
            VampireClassEnum.Gargantua => Color.FromHex("#d43a18"),
            VampireClassEnum.Dantalion => Color.FromHex("#6ab820"),
            VampireClassEnum.Bestia => Color.FromHex("#c43088"),
            _ => Color.FromHex("#e22218")
        };
    }

    #endregion

    #region Space Damage

    public bool ShouldTakeSpaceDamage(VampireComponent component, float frameTime, out bool shouldDamage)
    {
        shouldDamage = false;

        if (component.NextSpaceDamageTick <= 0)
        {
            shouldDamage = true;
            component.NextSpaceDamageTick = 1;
            return true;
        }

        component.NextSpaceDamageTick -= frameTime;
        return false;
    }

    #endregion

    #region Damage Sharing

    public DamageSpecifier GetSharedDamage(DamageSpecifier originalDamage, int participantCount)
    {
        if (participantCount <= 0)
            return originalDamage;

        return originalDamage / participantCount;
    }

    #endregion
}
