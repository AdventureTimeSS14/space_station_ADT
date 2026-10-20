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
/// Прототип одной зависимости: чем кормится, как копится и спадает, как выглядит ломка.
/// Системы кодом конкретные зависимости не различают - вся настройка здесь.
/// Id прототипа - то, что в yml пишется как kind.
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

    /// <summary>
    /// Если реаген подходит нескольким зависимостям, выигрывает та, у которой Priority выше.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// Рост уровня за цикл метаболизма источника - примерно раз в секунду, пока реаген в крови.
    /// </summary>
    [DataField]
    public float GainPerDose = 0.04f;

    /// <summary>
    /// Спад уровня в секунду без дозы, по умолчанию 100 за ~100 минут.
    /// </summary>
    [DataField]
    public float DecayPerSecond = 100f / 6000f;

    [DataField]
    public float MaxLevel = 100f;

    /// <summary>
    /// Ниже этого уровня зависимость считается вылеченной. Null - порог первой стадии.
    /// </summary>
    [DataField]
    public float? CuredBelow;

    /// <summary>
    /// Поп-ап первого превышения порога.
    /// </summary>
    [DataField]
    public LocId? BeginPopup;

    /// <summary>
    /// Поп-ап дозы, снявшей ломку.
    /// </summary>
    [DataField]
    public LocId? DosePopup;

    [DataField]
    public LocId? CuredPopup;

    [DataField]
    public PopupType LifecyclePopupType = PopupType.Small;

    /// <summary>
    /// Степень 0 - доза ещё держится, дальше номер стадии ломки до MaxAlertSeverity.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype>? Alert;

    /// <summary>
    /// Стадии выше рисуются той же картинкой.
    /// </summary>
    [DataField]
    public short MaxAlertSeverity = 2;

    /// <summary>
    /// Таймер до начала ломки на иконке, пока доза держится.
    /// </summary>
    [DataField]
    public bool AlertCooldown = true;

    [DataField]
    public AddictionWithdrawal Withdrawal = new();

    /// <summary>
    /// По возрастанию тяжести: Stages[0] - стадия 1. Минимум одна.
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

    /// <summary>
    /// Номер вне диапазона 1..Stages.Count зажимается.
    /// </summary>
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
    /// <summary>
    /// Сколько времени без дозы до начала ломки.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Нарастание тяжести по времени, отсчёт от конца Delay. Пусто - тяжесть сразу равна
    /// стадии зависимости. Нужно там, где уровень не спадает (неизлечимые трайты):
    /// иначе ломка прыгает в максимум сразу. Выше стадии зависимости не поднимается.
    /// </summary>
    [DataField]
    public List<AddictionRamp> Ramp = new();

    /// <summary>
    /// Доля Delay, после которой показывается предупреждение о нарастающей тяге.
    /// </summary>
    [DataField]
    public float? WarningAt;

    /// <summary>
    /// Варианты текста, выбирается случайный. Пусто - без предупреждения.
    /// </summary>
    [DataField]
    public List<LocId> WarningPopups = new();

    [DataField]
    public PopupType WarningPopupType = PopupType.Medium;
}

/// <summary>
/// Через After после начала ломки её тяжесть доходит до Stage.
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
    /// <summary>
    /// Уровень привыкания, с которого начинается стадия.
    /// </summary>
    [DataField(required: true)]
    public float Level;

    /// <summary>
    /// Выбирается случайный, соседние не повторяются.
    /// </summary>
    [DataField]
    public List<LocId> Popups = new();

    [DataField]
    public PopupType PopupType = PopupType.Small;

    [DataField]
    public TimeSpan PopupInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Держатся всё время ломки на этой стадии. Симптомы с одинаковым ключом от разных
    /// зависимостей не складываются: побеждает пришедший с более тяжёлой стадии.
    /// </summary>
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
    /// Шанс выдать эмоцию на каждом поп-апе ломки. 0 - не привязана к поп-апам.
    /// </summary>
    [DataField]
    public float PopupChance;

    /// <summary>
    /// Свой интервал, независимый от поп-апов. Первая эмоция - сразу при входе в стадию.
    /// </summary>
    [DataField]
    public TimeSpan? Interval;
}
