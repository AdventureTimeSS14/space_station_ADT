using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Content.Shared.Heretic;
using Content.Shared.Humanoid;
using Content.Shared.Item;
using Content.Shared.Nutrition.AnimalHusbandry;
using Content.Shared.Nutrition.Components;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Content.Shared.Temperature.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class GhoulSystem
{
    /// <summary>
    ///     Сохраняет все необходимые компоненты перед превращением в гуля.
    /// </summary>
    private GhoulStoredComponents SaveComponents(EntityUid ent)
    {
        var stored = new GhoulStoredComponents();
        Log.Info($"[GhoulSystem] Сохраняем компоненты для {ToPrettyString(ent)}");

        // Сохраняем Respirator
        if (TryComp<RespiratorComponent>(ent, out var respirator))
        {
            stored.Respirator = new RespiratorData
            {
                BreathVolume = respirator.BreathVolume,
                Ratio = respirator.Ratio,
                NextUpdate = respirator.NextUpdate,
                UpdateInterval = respirator.UpdateInterval,
                UpdateIntervalMultiplier = respirator.UpdateIntervalMultiplier,
                Saturation = respirator.Saturation,
                SuffocationThreshold = respirator.SuffocationThreshold,
                MaxSaturation = respirator.MaxSaturation,
                MinSaturation = respirator.MinSaturation,
                Damage = respirator.Damage != null ? new DamageSpecifier(respirator.Damage) : new DamageSpecifier(),
                DamageRecovery = respirator.DamageRecovery != null ? new DamageSpecifier(respirator.DamageRecovery) : new DamageSpecifier(),
                GaspEmoteCooldown = respirator.GaspEmoteCooldown,
                LastGaspEmoteTime = respirator.LastGaspEmoteTime,
                GaspEmote = respirator.GaspEmote,
                SuffocationCycles = respirator.SuffocationCycles,
                SuffocationCycleThreshold = respirator.SuffocationCycleThreshold,
                Status = respirator.Status,
            };
        }

        // Сохраняем Barotrauma
        if (TryComp<BarotraumaComponent>(ent, out var baro))
        {
            stored.Barotrauma = new BarotraumaData
            {
                Damage = baro.Damage != null ? new DamageSpecifier(baro.Damage) : new DamageSpecifier(),
                MaxDamage = baro.MaxDamage,
                ProtectionSlots = new List<string>(baro.ProtectionSlots),
                TakingDamage = baro.TakingDamage,
                HighPressureMultiplier = baro.HighPressureMultiplier,
                HighPressureModifier = baro.HighPressureModifier,
                LowPressureMultiplier = baro.LowPressureMultiplier,
                LowPressureModifier = baro.LowPressureModifier,
                HasImmunity = baro.HasImmunity,
                HighPressureAlert = baro.HighPressureAlert,
                LowPressureAlert = baro.LowPressureAlert,
                PressureAlertCategory = baro.PressureAlertCategory,
            };
        }

        // Сохраняем Hunger
        if (TryComp<HungerComponent>(ent, out var hunger))
        {
            stored.Hunger = new HungerData
            {
                LastAuthoritativeHungerValue = hunger.LastAuthoritativeHungerValue,
                LastAuthoritativeHungerChangeTime = hunger.LastAuthoritativeHungerChangeTime,
                BaseDecayRate = hunger.BaseDecayRate,
                ActualDecayRate = hunger.ActualDecayRate,
                LastThreshold = hunger.LastThreshold,
                CurrentThreshold = hunger.CurrentThreshold,
                Thresholds = new Dictionary<HungerThreshold, float>(hunger.Thresholds),
                HungerThresholdAlerts = hunger.HungerThresholdAlerts != null
                    ? new Dictionary<HungerThreshold, ProtoId<AlertPrototype>>(hunger.HungerThresholdAlerts)
                    : new Dictionary<HungerThreshold, ProtoId<AlertPrototype>>(),
                HungerAlertCategory = hunger.HungerAlertCategory,
                HungerThresholdDecayModifiers = hunger.HungerThresholdDecayModifiers != null
                    ? new Dictionary<HungerThreshold, float>(hunger.HungerThresholdDecayModifiers)
                    : new Dictionary<HungerThreshold, float>(),
                StarvingSlowdownModifier = hunger.StarvingSlowdownModifier,
                StarvationDamage = hunger.StarvationDamage,
                NextThresholdUpdateTime = hunger.NextThresholdUpdateTime,
                ThresholdUpdateRate = hunger.ThresholdUpdateRate,
            };
        }

        // Сохраняем Thirst
        if (TryComp<ThirstComponent>(ent, out var thirst))
        {
            stored.Thirst = new ThirstData
            {
                BaseDecayRate = thirst.BaseDecayRate,
                ActualDecayRate = thirst.ActualDecayRate,
                CurrentThirstThreshold = thirst.CurrentThirstThreshold,
                LastThirstThreshold = thirst.LastThirstThreshold,
                CurrentThirst = thirst.CurrentThirst,
                NextUpdateTime = thirst.NextUpdateTime,
                UpdateRate = thirst.UpdateRate,
                ThirstThresholds = new Dictionary<ThirstThreshold, float>(thirst.ThirstThresholds),
                ThirstyCategory = thirst.ThirstyCategory,
            };
        }

        // Сохраняем Reproductive
        if (TryComp<ReproductiveComponent>(ent, out var repro))
        {
            stored.Reproductive = new ReproductiveData
            {
                NextBreedAttempt = repro.NextBreedAttempt,
                MinBreedAttemptInterval = repro.MinBreedAttemptInterval,
                MaxBreedAttemptInterval = repro.MaxBreedAttemptInterval,
                BreedRange = repro.BreedRange,
                Capacity = repro.Capacity,
                BreedChance = repro.BreedChance,
                Offspring = repro.Offspring?.ToList() ?? new List<EntitySpawnEntry>(),
                Gestating = repro.Gestating,
                GestationEndTime = repro.GestationEndTime,
                GestationDuration = repro.GestationDuration,
                HungerPerBirth = repro.HungerPerBirth,
                BirthPopup = repro.BirthPopup,
                MakeOffspringInfant = repro.MakeOffspringInfant,
                PartnerWhitelist = repro.PartnerWhitelist != null
                    ? new EntityWhitelistData
                    {
                        Components = repro.PartnerWhitelist.Components,
                        Sizes = repro.PartnerWhitelist.Sizes != null
                            ? new List<ProtoId<ItemSizePrototype>>(repro.PartnerWhitelist.Sizes)
                            : null,
                        Tags = repro.PartnerWhitelist.Tags != null
                            ? new List<ProtoId<TagPrototype>>(repro.PartnerWhitelist.Tags)
                            : null,
                        RequireAll = repro.PartnerWhitelist.RequireAll,
                    }
                    : null,
            };
        }

        // Сохраняем ReproductivePartner
        if (HasComp<ReproductivePartnerComponent>(ent))
            stored.HasReproductivePartner = true;

        // Сохраняем Temperature
        if (TryComp<TemperatureComponent>(ent, out var temp))
        {
            stored.Temperature = new TemperatureData
            {
                CurrentTemperature = temp.CurrentTemperature,
                HeatDamageThreshold = temp.HeatDamageThreshold,
                ColdDamageThreshold = temp.ColdDamageThreshold,
                ParentHeatDamageThreshold = temp.ParentHeatDamageThreshold,
                ParentColdDamageThreshold = temp.ParentColdDamageThreshold,
                SpecificHeat = temp.SpecificHeat,
                AtmosTemperatureTransferEfficiency = temp.AtmosTemperatureTransferEfficiency,
                ColdDamage = new DamageSpecifier(temp.ColdDamage),
                HeatDamage = new DamageSpecifier(temp.HeatDamage),
                DamageCap = temp.DamageCap,
                TakingDamage = temp.TakingDamage,
                HotAlert = temp.HotAlert,
                ColdAlert = temp.ColdAlert,
            };
        }

        // Сохраняем оригинальный цвет кожи и глаз
        if (TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
        {
            stored.StoredSkinColor = humanoid.SkinColor;
            stored.StoredEyeColor = humanoid.EyeColor;
        }

        return stored;
    }

    /// <summary>
    ///     Восстанавливает сохранённые компоненты после смерти гуля.
    /// </summary>
    private void RestoreComponents(EntityUid ent, GhoulStoredComponents stored)
    {
        Log.Info($"[GhoulSystem] Восстанавливаем компоненты для {ToPrettyString(ent)}");
        Log.Info($"[GhoulSystem] Respirator: {stored.Respirator != null}, Barotrauma: {stored.Barotrauma != null}, Hunger: {stored.Hunger != null}, Thirst: {stored.Thirst != null}, Reproductive: {stored.Reproductive != null}, Temperature: {stored.Temperature != null}");
        Log.Info($"[GhoulSystem] SkinColor: {stored.StoredSkinColor}, EyeColor: {stored.StoredEyeColor}");

        // Восстанавливаем Respirator
        if (stored.Respirator != null)
        {
            var respirator = AddComp<RespiratorComponent>(ent);
            respirator.BreathVolume = stored.Respirator.BreathVolume;
            respirator.Ratio = stored.Respirator.Ratio;
            respirator.NextUpdate = stored.Respirator.NextUpdate;
            respirator.UpdateInterval = stored.Respirator.UpdateInterval;
            respirator.UpdateIntervalMultiplier = stored.Respirator.UpdateIntervalMultiplier;
            respirator.Saturation = stored.Respirator.Saturation;
            respirator.SuffocationThreshold = stored.Respirator.SuffocationThreshold;
            respirator.MaxSaturation = stored.Respirator.MaxSaturation;
            respirator.MinSaturation = stored.Respirator.MinSaturation;
            respirator.Damage = new DamageSpecifier(stored.Respirator.Damage);
            respirator.DamageRecovery = new DamageSpecifier(stored.Respirator.DamageRecovery);
            respirator.GaspEmoteCooldown = stored.Respirator.GaspEmoteCooldown;
            respirator.LastGaspEmoteTime = stored.Respirator.LastGaspEmoteTime;
            respirator.GaspEmote = stored.Respirator.GaspEmote;
            respirator.SuffocationCycles = stored.Respirator.SuffocationCycles;
            respirator.SuffocationCycleThreshold = stored.Respirator.SuffocationCycleThreshold;
            respirator.Status = stored.Respirator.Status;
        }

        // Восстанавливаем Barotrauma
        if (stored.Barotrauma != null)
        {
            var baro = AddComp<BarotraumaComponent>(ent);
            baro.Damage = new DamageSpecifier(stored.Barotrauma.Damage);
            baro.MaxDamage = stored.Barotrauma.MaxDamage;
            baro.ProtectionSlots = new List<string>(stored.Barotrauma.ProtectionSlots);
            baro.TakingDamage = stored.Barotrauma.TakingDamage;
            baro.HighPressureMultiplier = stored.Barotrauma.HighPressureMultiplier;
            baro.HighPressureModifier = stored.Barotrauma.HighPressureModifier;
            baro.LowPressureMultiplier = stored.Barotrauma.LowPressureMultiplier;
            baro.LowPressureModifier = stored.Barotrauma.LowPressureModifier;
            baro.HasImmunity = stored.Barotrauma.HasImmunity;
            baro.HighPressureAlert = stored.Barotrauma.HighPressureAlert;
            baro.LowPressureAlert = stored.Barotrauma.LowPressureAlert;
            baro.PressureAlertCategory = stored.Barotrauma.PressureAlertCategory;
        }

        // Восстанавливаем Hunger
        if (stored.Hunger != null)
        {
            var hunger = AddComp<HungerComponent>(ent);
            hunger.LastAuthoritativeHungerValue = stored.Hunger.LastAuthoritativeHungerValue;
            hunger.LastAuthoritativeHungerChangeTime = stored.Hunger.LastAuthoritativeHungerChangeTime;
            hunger.BaseDecayRate = stored.Hunger.BaseDecayRate;
            hunger.ActualDecayRate = stored.Hunger.ActualDecayRate;
            hunger.LastThreshold = stored.Hunger.LastThreshold;
            hunger.CurrentThreshold = stored.Hunger.CurrentThreshold;
            hunger.Thresholds = new Dictionary<HungerThreshold, float>(stored.Hunger.Thresholds);
            hunger.HungerThresholdAlerts = new Dictionary<HungerThreshold, ProtoId<AlertPrototype>>(stored.Hunger.HungerThresholdAlerts);
            hunger.HungerAlertCategory = stored.Hunger.HungerAlertCategory;
            hunger.HungerThresholdDecayModifiers = new Dictionary<HungerThreshold, float>(stored.Hunger.HungerThresholdDecayModifiers);
            hunger.StarvingSlowdownModifier = stored.Hunger.StarvingSlowdownModifier;
            hunger.StarvationDamage = stored.Hunger.StarvationDamage;
            hunger.NextThresholdUpdateTime = stored.Hunger.NextThresholdUpdateTime;
            hunger.ThresholdUpdateRate = stored.Hunger.ThresholdUpdateRate;
            Dirty(ent, hunger);
        }

        // Восстанавливаем Thirst
        if (stored.Thirst != null)
        {
            var thirst = AddComp<ThirstComponent>(ent);
            thirst.BaseDecayRate = stored.Thirst.BaseDecayRate;
            thirst.ActualDecayRate = stored.Thirst.ActualDecayRate;
            thirst.CurrentThirstThreshold = stored.Thirst.CurrentThirstThreshold;
            thirst.LastThirstThreshold = stored.Thirst.LastThirstThreshold;
            thirst.CurrentThirst = stored.Thirst.CurrentThirst;
            thirst.NextUpdateTime = stored.Thirst.NextUpdateTime;
            thirst.UpdateRate = stored.Thirst.UpdateRate;
            thirst.ThirstThresholds = new Dictionary<ThirstThreshold, float>(stored.Thirst.ThirstThresholds);
            thirst.ThirstyCategory = stored.Thirst.ThirstyCategory;
            Dirty(ent, thirst);
        }

        // Восстанавливаем Reproductive
        if (stored.Reproductive != null)
        {
            var repro = AddComp<ReproductiveComponent>(ent);
            repro.NextBreedAttempt = stored.Reproductive.NextBreedAttempt;
            repro.MinBreedAttemptInterval = stored.Reproductive.MinBreedAttemptInterval;
            repro.MaxBreedAttemptInterval = stored.Reproductive.MaxBreedAttemptInterval;
            repro.BreedRange = stored.Reproductive.BreedRange;
            repro.Capacity = stored.Reproductive.Capacity;
            repro.BreedChance = stored.Reproductive.BreedChance;
            repro.Offspring = stored.Reproductive.Offspring.ToList();
            repro.Gestating = stored.Reproductive.Gestating;
            repro.GestationEndTime = stored.Reproductive.GestationEndTime;
            repro.GestationDuration = stored.Reproductive.GestationDuration;
            repro.HungerPerBirth = stored.Reproductive.HungerPerBirth;
            repro.BirthPopup = stored.Reproductive.BirthPopup;
            repro.MakeOffspringInfant = stored.Reproductive.MakeOffspringInfant;
            if (stored.Reproductive.PartnerWhitelist != null)
            {
                repro.PartnerWhitelist ??= new EntityWhitelist();
                repro.PartnerWhitelist.Components = stored.Reproductive.PartnerWhitelist.Components;
                repro.PartnerWhitelist.Sizes = stored.Reproductive.PartnerWhitelist.Sizes;
                repro.PartnerWhitelist.Tags = stored.Reproductive.PartnerWhitelist.Tags;
                repro.PartnerWhitelist.RequireAll = stored.Reproductive.PartnerWhitelist.RequireAll;
            }
        }

        // Восстанавливаем ReproductivePartner
        if (stored.HasReproductivePartner)
            AddComp<ReproductivePartnerComponent>(ent);

        // Восстанавливаем Temperature
        if (stored.Temperature != null)
        {
            var temp = AddComp<TemperatureComponent>(ent);
            temp.CurrentTemperature = stored.Temperature.CurrentTemperature;
            temp.HeatDamageThreshold = stored.Temperature.HeatDamageThreshold;
            temp.ColdDamageThreshold = stored.Temperature.ColdDamageThreshold;
            temp.ParentHeatDamageThreshold = stored.Temperature.ParentHeatDamageThreshold;
            temp.ParentColdDamageThreshold = stored.Temperature.ParentColdDamageThreshold;
            temp.SpecificHeat = stored.Temperature.SpecificHeat;
            temp.AtmosTemperatureTransferEfficiency = stored.Temperature.AtmosTemperatureTransferEfficiency;
            temp.ColdDamage = new DamageSpecifier(stored.Temperature.ColdDamage);
            temp.HeatDamage = new DamageSpecifier(stored.Temperature.HeatDamage);
            temp.DamageCap = stored.Temperature.DamageCap;
            temp.TakingDamage = stored.Temperature.TakingDamage;
            temp.HotAlert = stored.Temperature.HotAlert;
            temp.ColdAlert = stored.Temperature.ColdAlert;
        }

        // Восстанавливаем цвет кожи и глаз
        if (stored.StoredSkinColor != null && TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
        {
            _humanoid.SetSkinColor(ent, stored.StoredSkinColor.Value, true, false, humanoid);
        }
        else
        {
            Log.Warning($"[GhoulSystem] НЕ удалось восстановить цвет кожи: StoredSkinColor={stored.StoredSkinColor}, HasHumanoid={HasComp<HumanoidAppearanceComponent>(ent)}");
        }
        if (stored.StoredEyeColor != null && TryComp<HumanoidAppearanceComponent>(ent, out humanoid))
        {
            Log.Info($"[GhoulSystem] Восстанавливаем цвет глаз: {stored.StoredEyeColor.Value}");
            humanoid.EyeColor = stored.StoredEyeColor.Value;
            Dirty(ent, humanoid);
        }
        else
        {
            Log.Warning($"[GhoulSystem] НЕ удалось восстановить цвет глаз: StoredEyeColor={stored.StoredEyeColor}, HasHumanoid={HasComp<HumanoidAppearanceComponent>(ent)}");
        }
    }
}
