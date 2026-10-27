// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Addiction.Sources;
using Content.Shared.ADT.Addiction.Symptoms;
using Content.Shared.Alert;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Настройки одной зависимости. Id прототипа - это kind в трайтах и эффектах.
/// </summary>
[Prototype("addiction")]
public sealed partial class AddictionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Реагенты, кормящие зависимость. Пусто - только через AdjustAddictionLevel.
    /// </summary>
    [DataField]
    public List<AddictionSource> Sources = new();

    [DataField]
    public int Priority;

    [DataField]
    public float GainPerDose = 0.04f;

    [DataField]
    public float DecayPerSecond = 100f / 6000f;

    [DataField]
    public float MaxLevel = 100f;

    /// <summary>
    /// Ниже этого уровня зависимость считается вылеченной. Null - порог первой стадии.
    /// </summary>
    [DataField]
    public float? CuredBelow;

    [DataField]
    public LocId? BeginPopup;

    [DataField]
    public LocId? DosePopup;

    [DataField]
    public LocId? CuredPopup;

    [DataField]
    public PopupType LifecyclePopupType = PopupType.Small;

    /// <summary>
    /// Степень 0 - доза ещё держится, дальше номер стадии ломки.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype>? Alert;

    [DataField]
    public short MaxAlertSeverity = 2;

    [DataField]
    public bool AlertCooldown = true;

    [DataField]
    public AddictionWithdrawal Withdrawal = new();

    /// <summary>
    /// По возрастанию тяжести: Stages[0] - стадия 1.
    /// </summary>
    [DataField(required: true)]
    public List<AddictionStageData> Stages = new();

    public float CureThreshold => CuredBelow ?? (Stages.Count > 0 ? Stages[0].Level : 0f);

    /// <summary>
    /// 0 - зависимости нет, дальше номер стадии.
    /// </summary>
    public int StageFromLevel(float level)
    {
        for (var i = Stages.Count - 1; i >= 0; i--)
        {
            if (level >= Stages[i].Level)
                return i + 1;
        }

        return 0;
    }

    public AddictionStageData? GetStage(int stage)
    {
        if (Stages.Count == 0)
            return null;

        return Stages[Math.Clamp(stage, 1, Stages.Count) - 1];
    }
}

[DataDefinition]
public sealed partial class AddictionWithdrawal
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Нарастание тяжести по времени от конца Delay. Пусто - тяжесть равна стадии зависимости.
    /// </summary>
    [DataField]
    public List<AddictionRamp> Ramp = new();

    /// <summary>
    /// Доля Delay, после которой показывается предупреждение.
    /// </summary>
    [DataField]
    public float? WarningAt;

    [DataField]
    public List<LocId> WarningPopups = new();

    [DataField]
    public PopupType WarningPopupType = PopupType.Medium;
}

/// <summary>
/// Через After после начала ломки тяжесть доходит до Stage.
/// </summary>
[DataDefinition]
public sealed partial class AddictionRamp
{
    [DataField(required: true)]
    public TimeSpan After;

    [DataField(required: true)]
    public int Stage;
}

[DataDefinition]
public sealed partial class AddictionStageData
{
    [DataField(required: true)]
    public float Level;

    [DataField]
    public List<LocId> Popups = new();

    [DataField]
    public PopupType PopupType = PopupType.Small;

    [DataField]
    public TimeSpan PopupInterval = TimeSpan.FromSeconds(60);

    [DataField]
    public List<AddictionSymptom> Symptoms = new();

    [DataField]
    public List<AddictionEmote> Emotes = new();
}

[DataDefinition]
public sealed partial class AddictionEmote
{
    [DataField(required: true)]
    public ProtoId<EmotePrototype> Emote;

    /// <summary>
    /// 0 - эмоция не привязана к поп-апам.
    /// </summary>
    [DataField]
    public float PopupChance;

    /// <summary>
    /// Свой интервал, независимый от поп-апов.
    /// </summary>
    [DataField]
    public TimeSpan? Interval;
}
