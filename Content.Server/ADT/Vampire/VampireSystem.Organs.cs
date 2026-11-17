// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using System.Linq;
using Content.Shared.Body;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Metabolism;
using Content.Shared.ADT.Vampire.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.ADT.Vampire;

namespace Content.Server.ADT.Vampire;

public sealed partial class VampireSystem
{
    private static readonly ProtoId<MetabolismStagePrototype>[] CriticalStages = new ProtoId<MetabolismStagePrototype>[]
    {
        "Bloodstream", "Respiration"
    };

    private static readonly ProtoId<OrganCategoryPrototype> Eyes = "Eyes";
    private static readonly ProtoId<OrganCategoryPrototype> EarsCategory = "Ears";

    private void InitializeOrgans()
    {
        SubscribeLocalEvent<BestiaContainerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BestiaContainerComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(EntityUid uid, BestiaContainerComponent component, ref MapInitEvent args)
    {
        component.OrgansContainer = _container.EnsureContainer<Container>(uid, BestiaContainerComponent.ContainerId);
    }

    private void OnShutdown(EntityUid uid, BestiaContainerComponent component, ref ComponentShutdown args)
    {
        var container = component.OrgansContainer;
        if (container.ContainedEntities.Count == 0)
            return;

        var coords = Transform(uid).Coordinates;
        foreach (var organ in container.ContainedEntities)
        {
            _container.Remove(organ, container, force: true, destination: coords);
            _throwing.TryThrow(organ, _random.NextVector2(), 5f);
        }
    }

    #region Eui State

    public TrophiesEuiState? GetTrophiesState(Entity<VampireComponent?, BestiaContainerComponent?> vampire)
    {
        if (!Resolve(vampire, ref vampire.Comp1, false) || !Resolve(vampire, ref vampire.Comp2, false))
            return null;

        var organs = GetOrganDisplayInfo(vampire.Comp2);
        var passives = GetPassiveBonuses((vampire.Owner, vampire.Comp2));
        var abilities = GetAbilityBonuses(vampire.Owner, vampire.Comp1, vampire.Comp2);

        return new TrophiesEuiState(organs, passives, abilities);
    }

    private List<OrganDisplayInfo> GetOrganDisplayInfo(BestiaContainerComponent bestia)
    {
        var result = new List<OrganDisplayInfo>();
        var counts = GetOrganCounts(bestia.OrgansContainer);
        var maxCritical = bestia.MaxCriticalOrgans;
        var maxRegular = bestia.MaxRegularOrgans;

        foreach (BestiaOrganType type in Enum.GetValues<BestiaOrganType>())
        {
            if (type == BestiaOrganType.Unknown)
                continue;

            var count = counts.GetValueOrDefault(type, 0);
            var max = IsCriticalOrganType(type) ? maxCritical : maxRegular;
            var (color, _) = GetOrganProgressColor(count, max);
            var preview = GetPreviewForOrganType(bestia.OrgansContainer, type);

            result.Add(new OrganDisplayInfo
            {
                Type = type,
                Count = count,
                MaxCount = max,
                CountColor = color,
                PreviewEntity = preview
            });
        }

        return result;
    }

    private List<PassiveBonusInfo> GetPassiveBonuses(Entity<BestiaContainerComponent> entity)
    {
        var heartCount = GetOrganTypeCount(entity, BestiaOrganType.Heart);
        var liverCount = GetOrganTypeCount(entity, BestiaOrganType.Liver);
        var lungsCount = GetOrganTypeCount(entity, BestiaOrganType.Lungs);
        var kidneysCount = GetOrganTypeCount(entity, BestiaOrganType.Kidneys);
        var eyesCount = GetOrganTypeCount(entity, BestiaOrganType.Eyes);
        var stomachCount = GetOrganTypeCount(entity, BestiaOrganType.Stomach);

        var maxCritical = entity.Comp.MaxCriticalOrgans;
        var maxRegular = entity.Comp.MaxRegularOrgans;

        var passives = new List<PassiveBonusInfo>();

        var heartColor = GetOrganProgressColor(heartCount, maxCritical);
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-brute-protection"),
            Value = $"-{(heartCount * 5)}%",
            ValueColor = heartColor.color,
            IsMaxed = heartColor.isMaxed
        });
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-burn-protection"),
            Value = $"-{(heartCount * 5)}%",
            ValueColor = heartColor.color,
            IsMaxed = heartColor.isMaxed
        });

        var lungsColor = GetOrganProgressColor(lungsCount, maxCritical);
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-oxy-protection"),
            Value = $"-{(lungsCount * 5)}%",
            ValueColor = lungsColor.color,
            IsMaxed = lungsColor.isMaxed
        });
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-stamina"),
            Value = $"+{(lungsCount * 5)}%",
            ValueColor = lungsColor.color,
            IsMaxed = lungsColor.isMaxed
        });

        var liverColor = GetOrganProgressColor(liverCount, maxRegular);
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-toxin-protection"),
            Value = $"-{(liverCount * 3)}%",
            ValueColor = liverColor.color,
            IsMaxed = liverColor.isMaxed
        });
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-blood-cost-reduction"),
            Value = $"-{(liverCount * 2)}%",
            ValueColor = liverColor.color,
            IsMaxed = liverColor.isMaxed
        });

        var kidneysColor = GetOrganProgressColor(kidneysCount, maxRegular);
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-suck-rate"),
            Value = $"-{(kidneysCount * 0.3)}s",
            ValueColor = kidneysColor.color,
            IsMaxed = kidneysColor.isMaxed
        });
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-cellular-protection"),
            Value = $"-{(kidneysCount * 3)}%",
            ValueColor = kidneysColor.color,
            IsMaxed = kidneysColor.isMaxed
        });

        var eyesColor = GetOrganProgressColor(eyesCount, maxRegular);
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-flash-protection"),
            Value = eyesCount > 1 ? Loc.GetString("vampire-bestia-passive-unlocked")
                : Loc.GetString("vampire-bestia-passive-locked"),
            ValueColor = eyesCount > 1 ? eyesColor.color : Color.Orange,
            IsMaxed = eyesColor.isMaxed
        });
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-welding-protection"),
            Value = eyesCount > 3 ? Loc.GetString("vampire-bestia-passive-unlocked")
                : Loc.GetString("vampire-bestia-passive-locked"),
            ValueColor = eyesCount > 3 ? eyesColor.color : Color.Orange,
            IsMaxed = eyesColor.isMaxed
        });
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-xray-vision"),
            Value = eyesCount > 7 ? Loc.GetString("vampire-bestia-passive-unlocked")
                : Loc.GetString("vampire-bestia-passive-locked"),
            ValueColor = eyesCount > 7 ? eyesColor.color : Color.Orange,
            IsMaxed = eyesColor.isMaxed
        });

        var earsCount = GetOrganTypeCount(entity, BestiaOrganType.Ears);
        var earsColor = GetOrganProgressColor(earsCount, maxRegular);
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-bang-protection"),
            Value = earsCount > 3 ? Loc.GetString("vampire-bestia-passive-unlocked")
                : Loc.GetString("vampire-bestia-passive-locked"),
            ValueColor = earsCount > 3 ? earsColor.color : Color.Orange,
            IsMaxed = earsColor.isMaxed
        });

        var stomachColor = GetOrganProgressColor(stomachCount, maxRegular);
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-blood-gain"),
            Value = $"+{(stomachCount * 0.25)}",
            ValueColor = stomachColor.color,
            IsMaxed = stomachColor.isMaxed
        });
        passives.Add(new()
        {
            Name = Loc.GetString("vampire-bestia-passive-healing-efficiency"),
            Value = $"+{(stomachCount * 3)}%",
            ValueColor = stomachColor.color,
            IsMaxed = stomachColor.isMaxed
        });

        return passives;
    }

    private List<AbilityDisplayInfo> GetAbilityBonuses(EntityUid uid, VampireComponent vampire, BestiaContainerComponent bestia)
    {
        var result = new List<AbilityDisplayInfo>();
        var skills = vampire.AcquiredSkills.Keys.ToList();

        var targetActions = new HashSet<EntProtoId>
        {
            "ActionVampireInfectedTrophy",
            "ActionVampireLunge",
            "ActionVampireMarkPrey",
            "ActionVampireMetamorphosisBats",
            "ActionVampireSummonBats",
            "ActionVampireMetamorphosisHound"
        };

        foreach (var actionId in skills)
        {
            if (!targetActions.Contains(actionId))
                continue;

            var actionEntity = vampire.AcquiredSkills.GetValueOrDefault(actionId);
            if (actionEntity == null)
                continue;

            var name = Name(actionEntity.Value) ?? actionId;
            var bonuses = GetBonusesForAbility(uid, bestia, actionId);

            result.Add(new AbilityDisplayInfo
            {
                Action = GetNetEntity(actionEntity.Value),
                Name = name,
                Bonuses = bonuses
            });
        }

        return result;
    }

    private List<OrganBonusDetail> GetBonusesForAbility(EntityUid uid, BestiaContainerComponent bestia, EntProtoId actionId)
    {
        var heart = GetOrganTypeCount((uid, bestia), BestiaOrganType.Heart);
        var lungs = GetOrganTypeCount((uid, bestia), BestiaOrganType.Lungs);
        var liver = GetOrganTypeCount((uid, bestia), BestiaOrganType.Liver);
        var kidneys = GetOrganTypeCount((uid, bestia), BestiaOrganType.Kidneys);
        var eyes = GetOrganTypeCount((uid, bestia), BestiaOrganType.Eyes);
        var stomach = GetOrganTypeCount((uid, bestia), BestiaOrganType.Stomach);

        var maxCritical = bestia.MaxCriticalOrgans;
        var maxRegular = bestia.MaxRegularOrgans;

        var bonuses = new List<OrganBonusDetail>();

        switch (actionId)
        {
            case "ActionVampireInfectedTrophy":
                {
                    var heartBonus = heart * 0.5f;
                    var liverBonus = Math.Min(40, liver * 3);
                    var eyesBonus = Math.Min(0.525f, eyes * 0.035f);
                    var stomachBonus = Math.Min(5f, stomach);

                    AddBonus(bonuses, BestiaOrganType.Heart,
                        Loc.GetString("vampire-bestia-bonus-infected-trophy-heart", ("value", heartBonus)), heart, maxCritical);
                    AddBonus(bonuses, BestiaOrganType.Liver,
                        Loc.GetString("vampire-bestia-bonus-infected-trophy-liver", ("value", liverBonus)), liver, maxRegular);
                    AddBonus(bonuses, BestiaOrganType.Eyes,
                        Loc.GetString("vampire-bestia-bonus-infected-trophy-eyes", ("value", eyesBonus.ToString("F2"))), eyes, maxRegular);
                    AddBonus(bonuses, BestiaOrganType.Stomach,
                        Loc.GetString("vampire-bestia-bonus-infected-trophy-stomach", ("value", stomachBonus)), stomach, maxRegular);
                    break;
                }
            case "ActionVampireLunge":
                {
                    var heartBonus = heart * 0.5f;
                    var lungsBonus = Math.Min(11f, 5f + lungs);
                    var kidneysBonus = Math.Min(50, 5 + kidneys * 5);
                    var stomachBonus = Math.Min(1.5f, 0.5f + stomach * 0.5f);

                    AddBonus(bonuses, BestiaOrganType.Heart,
                        Loc.GetString("vampire-bestia-bonus-lunge-heart", ("value", heartBonus)), heart, maxCritical);
                    AddBonus(bonuses, BestiaOrganType.Lungs,
                        Loc.GetString("vampire-bestia-bonus-lunge-lungs", ("value", lungsBonus)), lungs, maxCritical);
                    AddBonus(bonuses, BestiaOrganType.Kidneys,
                        Loc.GetString("vampire-bestia-bonus-lunge-kidneys", ("value", kidneysBonus)), kidneys, maxRegular);
                    AddBonus(bonuses, BestiaOrganType.Stomach,
                        Loc.GetString("vampire-bestia-bonus-lunge-stomach", ("value", stomachBonus)), stomach, maxRegular);
                    break;
                }
            case "ActionVampireMarkPrey":
                {
                    var heartDamage = Math.Min(6, heart);
                    var heartChance = Math.Min(60, heart * 10);
                    var eyesBonus = Math.Min(8f, 3f + eyes * 0.5f);
                    var kidneysBonus = Math.Min(15, 5 + kidneys);

                    AddBonus(bonuses, BestiaOrganType.Heart,
                        Loc.GetString("vampire-bestia-bonus-mark-prey-heart", ("damage", heartDamage), ("chance", heartChance)), heart, maxCritical);
                    AddBonus(bonuses, BestiaOrganType.Eyes,
                        Loc.GetString("vampire-bestia-bonus-mark-prey-eyes", ("value", eyesBonus)), eyes, maxRegular);
                    AddBonus(bonuses, BestiaOrganType.Kidneys,
                        Loc.GetString("vampire-bestia-bonus-mark-prey-kidneys", ("value", kidneysBonus)), kidneys, maxRegular);
                    break;
                }
            case "ActionVampireMetamorphosisBats":
                {
                    var heartHealth = Math.Min(250f, 130f + heart * 20f);
                    var heartDamage = Math.Min(8f, heart * 0.75f);
                    var lungsBonus = Math.Min(30, lungs * 5);
                    var liverBonus = Math.Min(3f, liver * 0.5f);
                    var kidneysBonus = Math.Min(10, 1 + kidneys);

                    AddBonus(bonuses, BestiaOrganType.Heart,
                        Loc.GetString("vampire-bestia-bonus-metamorphosis-bats-heart", ("health", heartHealth), ("damage", heartDamage)), heart, maxCritical);
                    AddBonus(bonuses, BestiaOrganType.Lungs,
                        Loc.GetString("vampire-bestia-bonus-metamorphosis-bats-lungs", ("value", lungsBonus)), lungs, maxCritical);
                    AddBonus(bonuses, BestiaOrganType.Liver,
                        Loc.GetString("vampire-bestia-bonus-metamorphosis-bats-liver", ("value", liverBonus)), liver, maxRegular);
                    AddBonus(bonuses, BestiaOrganType.Kidneys,
                        Loc.GetString("vampire-bestia-bonus-metamorphosis-bats-kidneys", ("value", kidneysBonus)), kidneys, maxRegular);
                    break;
                }
            case "ActionVampireSummonBats":
                {
                    var heartHealth = Math.Min(140f, 80f + heart * 10f);
                    var heartDamage = Math.Min(6f, heart * 0.75f);
                    var lungsBonus = Math.Min(60, lungs * 10);
                    var liverBonus = Math.Min(10f, liver * 0.5f);
                    var kidneysBonus = kidneys;

                    AddBonus(bonuses, BestiaOrganType.Heart,
                        Loc.GetString("vampire-bestia-bonus-summon-bats-heart", ("health", heartHealth), ("damage", heartDamage)), heart, maxCritical);
                    AddBonus(bonuses, BestiaOrganType.Lungs,
                        Loc.GetString("vampire-bestia-bonus-summon-bats-lungs", ("value", lungsBonus)), lungs, maxCritical);
                    AddBonus(bonuses, BestiaOrganType.Liver,
                        Loc.GetString("vampire-bestia-bonus-summon-bats-liver", ("value", liverBonus)), liver, maxRegular);
                    AddBonus(bonuses, BestiaOrganType.Kidneys,
                        Loc.GetString("vampire-bestia-bonus-summon-bats-kidneys", ("value", kidneysBonus)), kidneys, maxRegular);
                    break;
                }
            case "ActionVampireMetamorphosisHound":
                {
                    var heartHealth = Math.Min(320f, 140f + heart * 30f);
                    var heartDamage = Math.Min(6f, heart);
                    var lungsBonus = Math.Min(30, lungs * 5);
                    var kidneysBonus = kidneys;
                    var liverBonus = Math.Min(10f, liver * 0.5f);

                    AddBonus(bonuses, BestiaOrganType.Heart,
                        Loc.GetString("vampire-bestia-bonus-metamorphosis-hound-heart", ("health", heartHealth), ("damage", heartDamage)), heart, maxCritical);
                    AddBonus(bonuses, BestiaOrganType.Lungs,
                        Loc.GetString("vampire-bestia-bonus-metamorphosis-hound-lungs", ("value", lungsBonus)), lungs, maxCritical);
                    AddBonus(bonuses, BestiaOrganType.Kidneys,
                        Loc.GetString("vampire-bestia-bonus-metamorphosis-hound-kidneys", ("value", kidneysBonus)), kidneys, maxRegular);
                    AddBonus(bonuses, BestiaOrganType.Liver,
                        Loc.GetString("vampire-bestia-bonus-metamorphosis-hound-liver", ("value", liverBonus)), liver, maxRegular);
                    break;
                }
        }

        return bonuses;
    }

    private void AddBonus(List<OrganBonusDetail> list, BestiaOrganType organType, string description, int count, int max)
    {
        var (_, isMaxed) = GetOrganProgressColor(count, max);
        list.Add(new OrganBonusDetail
        {
            OrganType = Loc.GetString($"vampire-bestia-{organType.ToString().ToLower()}"),
            Description = description,
            IsMaxed = isMaxed
        });
    }

    private NetEntity? GetPreviewForOrganType(Container container, BestiaOrganType type)
    {
        var organsOfType = new List<EntityUid>();
        foreach (var organ in container.ContainedEntities)
        {
            if (GetOrganTypeEnum(organ) == type)
                organsOfType.Add(organ);
        }
        return organsOfType.Count > 0 ? GetNetEntity(_random.Pick(organsOfType)) : null;
    }

    private Dictionary<BestiaOrganType, int> GetOrganCounts(Container container)
    {
        var counts = new Dictionary<BestiaOrganType, int>();
        foreach (var organ in container.ContainedEntities)
        {
            var type = GetOrganTypeEnum(organ);
            if (type != BestiaOrganType.Unknown)
                counts[type] = counts.GetValueOrDefault(type) + 1;
        }
        return counts;
    }

    private bool IsCriticalOrganType(BestiaOrganType type)
    {
        return type == BestiaOrganType.Heart || type == BestiaOrganType.Lungs;
    }

    private (Color color, bool isMaxed) GetOrganProgressColor(int current, int max)
    {
        if (current <= 0) return (Color.Orange, false);

        if (current >= max) return (Color.YellowGreen, true);

        var t = (float)current / max;
        var color = Color.InterpolateBetween(Color.Orange, Color.YellowGreen, t);
        return (color, false);
    }

    #endregion

    #region Organs Manipulation

    private List<NetEntity> GetAvailableOrgans(EntityUid vampire, EntityUid target, BestiaContainerComponent? bestia = null)
    {
        var organs = new List<NetEntity>();
        if (!Resolve(vampire, ref bestia, false))
            return organs;

        if (!TryComp<BodyComponent>(target, out var body) || body.Organs == null)
            return organs;

        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (!(TryComp<OrganComponent>(organ, out var organComp) && (organComp.Category == Eyes || organComp.Category == EarsCategory))
                && !HasComp<MetabolizerComponent>(organ))
                continue;

            var organType = GetOrganTypeEnum(organ);
            if (organType == BestiaOrganType.Unknown)
                continue;

            var currentCount = GetOrganTypeCount((vampire, bestia), organType);
            var maxCount = IsCriticalOrgan(organ) ? bestia.MaxCriticalOrgans : bestia.MaxRegularOrgans;
            if (currentCount >= maxCount)
                continue;

            organs.Add(GetNetEntity(organ));
        }

        return organs;
    }

    private int GetOrganTypeCount(Entity<BestiaContainerComponent> entity, BestiaOrganType organType)
    {
        var counts = GetOrganCounts(entity.Comp.OrgansContainer);
        return counts.GetValueOrDefault(organType, 0);
    }

    private int GetOrganTypeCount(EntityUid uid, BestiaOrganType organType, BestiaContainerComponent? bestia = null)
    {
        if (!Resolve(uid, ref bestia, false))
            return 0;

        var counts = GetOrganCounts(bestia.OrgansContainer);
        return counts.GetValueOrDefault(organType, 0);
    }

    private BestiaOrganType GetOrganTypeEnum(EntityUid organ, OrganComponent? organComp = null)
    {
        if (Resolve(organ, ref organComp, false) && organComp.Category != null)
        {
            var id = organComp.Category.Value.Id;
            return id switch
            {
                "Heart" => BestiaOrganType.Heart,
                "Lungs" => BestiaOrganType.Lungs,
                "Liver" => BestiaOrganType.Liver,
                "Kidneys" => BestiaOrganType.Kidneys,
                "Eyes" => BestiaOrganType.Eyes,
                "Stomach" => BestiaOrganType.Stomach,
                "Ears" => BestiaOrganType.Ears,
                _ => BestiaOrganType.Unknown
            };
        }

        return BestiaOrganType.Unknown;
    }

    private bool IsCriticalOrgan(EntityUid organ, MetabolizerComponent? metabolizer = null)
    {
        if (!Resolve(organ, ref metabolizer, false) || metabolizer.Stages == null)
            return false;

        var stages = metabolizer.Stages;
        foreach (var stage in CriticalStages)
        {
            if (stages.Contains(stage.Id))
                return true;
        }

        return false;
    }

    #endregion

    private void UpdateBestiaLimits(Entity<VampireComponent?, BestiaContainerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp1, false) || !Resolve(ent, ref ent.Comp2, false))
            return;

        var blood = ent.Comp1.CurrentBlood.Float();

        var bestia = ent.Comp2;
        var sortedCritical = bestia.CriticalOrganThresholds.Keys.OrderBy(x => x).ToList();
        foreach (var threshold in sortedCritical)
        {
            if (blood >= threshold && !bestia.UnlockedCriticalThresholds.Contains(threshold))
            {
                bestia.UnlockedCriticalThresholds.Add(threshold);
                bestia.MaxCriticalOrgans += bestia.CriticalOrganThresholds[threshold];
            }
        }

        var sortedVictim = bestia.OrgansPerVictimThresholds.Keys.OrderBy(x => x).ToList();
        foreach (var threshold in sortedVictim)
        {
            if (blood >= threshold && !bestia.UnlockedVictimThresholds.Contains(threshold))
            {
                bestia.UnlockedVictimThresholds.Add(threshold);
                bestia.MaxOrgansPerVictim += bestia.OrgansPerVictimThresholds[threshold];
            }
        }
    }

    private void UpdateProtections(Entity<BestiaContainerComponent> ent)
    {
        var eyesCount = GetOrganTypeCount(ent, BestiaOrganType.Eyes);
        var earsCount = GetOrganTypeCount(ent, BestiaOrganType.Ears);

        if ((eyesCount >= 2 || earsCount >= 4) && !HasComp<FlashImmunityComponent>(ent))
            EnsureComp<FlashImmunityComponent>(ent);

        if (eyesCount >= 4 && !HasComp<EyeProtectionComponent>(ent))
            EnsureComp<EyeProtectionComponent>(ent);

        if (TryComp<VampireComponent>(ent, out var vampire))
            UpdateXrayVision((ent.Owner, vampire));
    }
}
