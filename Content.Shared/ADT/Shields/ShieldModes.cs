using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Shields;

/// <summary>Битовые флаги: что поле блокирует и как себя ведёт.</summary>
[Flags, Serializable]
public enum ShieldModes
{
    None = 0,
    Hyperkinetic = 1 << 0,
    Photonic = 1 << 1,
    Humanoids = 1 << 3,
    Anorganic = 1 << 4,
    Atmospheric = 1 << 5,
    Bypass = 1 << 7,
    Overcharge = 1 << 8,
    Modulate = 1 << 9,
}

/// <summary>Категории урона, которые понимает щит.</summary>
public enum ShieldDamType : byte
{
    Physical,
    Em,
    Heat,
}

/// <summary>Состояние генератора</summary>
public enum ShieldRunningState : byte
{
    Off,
    Discharging,
    Running,
}

/// <summary>Результат попадания по щиту.</summary>
public enum ShieldHitLevel : byte
{
    Absorbed,
    Minor,
    Major,
    Critical,
    Failure,
}

/// <summary>Описание режима для UI и расчёта апкипа.</summary>
public readonly record struct ShieldModeInfo(
    ShieldModes Flag,
    float Multiplier,
    string Name,
    string Description,
    bool HackedOnly);

public static class ShieldConstants
{
    /// <summary>Джоулей из резерва генератора за 1 единицу урона.</summary>
    public const float EnergyPerHp = 50000.0f;

    /// <summary>Базовый апкип за тайл в секунду, умножается на множители режимов.</summary>
    public const float EnergyUpkeepPerTile = 35f;

    /// <summary>Максимальная митигация, проценты.</summary>
    public const float MaxMitigationBase = 50f;

    /// <summary>Прирост митигации за удар соответствующего типа.</summary>
    public const float MitigationHitGain = 5f;

    /// <summary>Потеря митигации при каждом ударе, по всем типам.</summary>
    public const float MitigationHitLoss = 4f;

    /// <summary>Пассивная потеря митигации в секунду.</summary>
    public const float MitigationLossPassive = 0.5f;

    /// <summary>Скорость рассеивания энергии при плавном выключении.</summary>
    public const float ShutdownDispersionRate = 400000.0f;

    /// <summary>Запасное время простоя, если у подавленного тайла нет длительности.</summary>
    public const float DiffuseRefreshBase = 5f;
}

public static class ShieldModesHelpers
{
    public static readonly IReadOnlyList<ShieldModeInfo> AllModes = new[]
    {
        new ShieldModeInfo(ShieldModes.Hyperkinetic, 1.2f, "shield-mode-hyperkinetic", "shield-mode-hyperkinetic-desc", false),
        new ShieldModeInfo(ShieldModes.Photonic, 1.3f, "shield-mode-photonic", "shield-mode-photonic-desc", false),
        new ShieldModeInfo(ShieldModes.Humanoids, 1.5f, "shield-mode-humanoids", "shield-mode-humanoids-desc", false),
        new ShieldModeInfo(ShieldModes.Anorganic, 1.5f, "shield-mode-anorganic", "shield-mode-anorganic-desc", false),
        new ShieldModeInfo(ShieldModes.Atmospheric, 1.3f, "shield-mode-atmospheric", "shield-mode-atmospheric-desc", false),
        new ShieldModeInfo(ShieldModes.Modulate, 2f, "shield-mode-modulate", "shield-mode-modulate-desc", false),
        new ShieldModeInfo(ShieldModes.Bypass, 3f, "shield-mode-bypass", "shield-mode-bypass-desc", true),
        new ShieldModeInfo(ShieldModes.Overcharge, 3f, "shield-mode-overcharge", "shield-mode-overcharge-desc", true),
    };

    public static bool HasMode(this ShieldModes modes, ShieldModes flag)
        => (modes & flag) != 0;

    public static float GetUpkeepMultiplier(this ShieldModes modes)
    {
        var multiplier = 1f;
        foreach (var mode in AllModes)
        {
            if (modes.HasMode(mode.Flag))
                multiplier *= mode.Multiplier;
        }
        return multiplier;
    }
}