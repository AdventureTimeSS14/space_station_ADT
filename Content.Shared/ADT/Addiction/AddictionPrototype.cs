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
/// Вся настройка живёт здесь, системы кодом конкретные зависимости не различают.
/// Id прототипа - то, что в yml пишется как kind (трайты, AdjustAddictionLevel).
/// </summary>
[Prototype("addiction")]
public sealed partial class AddictionPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Название для UI (анализатор здоровья).
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Реагенты, кормящие зависимость. Пусто - кормить можно только эффектом AdjustAddictionLevel
    /// (так живут лекарственные зависимости: они появляются от передоза, а не от самого факта приёма).
    /// </summary>
    [DataField]
    public List<AddictionSource> Sources = new();

    /// <summary>
    /// Приоритет при разборе реагента: если реаген подходит нескольким зависимостям,
    /// выигрывает та, у которой Priority выше.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// Рост уровня за цикл метаболизма реагента-источника (примерно раз в секунду, пока реаген в крови).
    /// </summary>
    [DataField]
    public float GainPerDose = 0.04f;

    /// <summary>
    /// Спад уровня в секунду без дозы. По умолчанию 100 за ~100 минут воздержания.
    /// Неизлечимые (Permanent) каналы не спадают.
    /// </summary>
    [DataField]
    public float DecayPerSecond = 100f / 6000f;

    /// <summary>
    /// Потолок уровня привыкания.
    /// </summary>
    [DataField]
    public float MaxLevel = 100f;

    /// <summary>
    /// Ниже этого уровня зависимость считается вылеченной. Null - порог первой стадии.
    /// </summary>
    [DataField]
    public float? CuredBelow;

    /// <summary>
    /// Поп-ап первого превышения порога (подсадка).
    /// </summary>
    [DataField]
    public LocId? BeginPopup;

    /// <summary>
    /// Поп-ап дозы, снявшей ломку.
    /// </summary>
    [DataField]
    public LocId? DosePopup;

    /// <summary>
    /// Поп-ап выздоровления (уровень упал ниже порога).
    /// </summary>
    [DataField]
    public LocId? CuredPopup;

    /// <summary>
    /// Тип поп-апов подсадки, дозы и выздоровления.
    /// </summary>
    [DataField]
    public PopupType LifecyclePopupType = PopupType.Small;

    /// <summary>
    /// Иконка состояния справа. Null - зависимость без иконки.
    /// Степени: 0 - доза держится (с таймером до ломки), дальше номер стадии ломки до MaxAlertSeverity.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype>? Alert;

    /// <summary>
    /// Максимальная степень иконки: стадии выше рисуются той же картинкой.
    /// </summary>
    [DataField]
    public short MaxAlertSeverity = 2;

    /// <summary>
    /// Показывать на иконке таймер до начала ломки, пока доза держится.
    /// </summary>
    [DataField]
    public bool AlertCooldown = true;

    [DataField]
    public AddictionWithdrawal Withdrawal = new();

    /// <summary>
    /// Стадии по возрастанию тяжести: Stages[0] - стадия 1. Минимум одна.
    /// </summary>
    [DataField(required: true)]
    public List<AddictionStageData> Stages = new();

    /// <summary>
    /// Уровень, ниже которого зависимость отпускает.
    /// </summary>
    public float CureThreshold => CuredBelow ?? (Stages.Count > 0 ? Stages[0].Level : 0f);

    /// <summary>
    /// Стадия зависимости по уровню привыкания: 0 - нет, дальше номер стадии.
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
    /// Данные стадии по её номеру (1..Stages.Count). Номер вне диапазона зажимается.
    /// </summary>
    public AddictionStageData? GetStage(int stage)
    {
        if (Stages.Count == 0)
            return null;

        return Stages[Math.Clamp(stage, 1, Stages.Count) - 1];
    }
}

/// <summary>
/// Настройки ломки: когда начинается и как нарастает тяжесть.
/// </summary>
[DataDefinition]
public sealed partial class AddictionWithdrawal
{
    /// <summary>
    /// Сколько времени без дозы до начала ломки.
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Нарастание тяжести по времени без дозы (отсчёт от конца Delay).
    /// Пусто - тяжесть сразу равна стадии самой зависимости.
    /// Нужно там, где уровень не спадает (неизлечимые трайты): иначе ломка прыгает в максимум сразу.
    /// Выше стадии самой зависимости не поднимается.
    /// </summary>
    [DataField]
    public List<AddictionRamp> Ramp = new();

    /// <summary>
    /// Доля Delay, после которой показывается предупреждение о нарастающей тяге.
    /// Null - без предупреждения.
    /// </summary>
    [DataField]
    public float? WarningAt;

    /// <summary>
    /// Варианты текста предупреждения, выбирается случайный. Пусто - без предупреждения.
    /// </summary>
    [DataField]
    public List<LocId> WarningPopups = new();

    [DataField]
    public PopupType WarningPopupType = PopupType.Medium;
}

/// <summary>
/// Ступень нарастания ломки: через After после начала ломки тяжесть доходит до Stage.
/// </summary>
[DataDefinition]
public sealed partial class AddictionRamp
{
    [DataField(required: true)]
    public TimeSpan After;

    [DataField(required: true)]
    public int Stage;
}

/// <summary>
/// Одна стадия зависимости: с какого уровня начинается, чем даёт о себе знать в ломке.
/// </summary>
[DataDefinition]
public sealed partial class AddictionStageData
{
    /// <summary>
    /// Уровень привыкания, с которого начинается стадия.
    /// </summary>
    [DataField(required: true)]
    public float Level;

    /// <summary>
    /// Варианты текста поп-апа ломки. Выбирается случайный, соседние не повторяются.
    /// Пусто - стадия молчит.
    /// </summary>
    [DataField]
    public List<LocId> Popups = new();

    [DataField]
    public PopupType PopupType = PopupType.Small;

    /// <summary>
    /// Интервал между поп-апами ломки на этой стадии.
    /// </summary>
    [DataField]
    public TimeSpan PopupInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Симптомы, которые держатся всё время ломки на этой стадии.
    /// Симптомы с одинаковым ключом от разных зависимостей не складываются:
    /// побеждает пришедший с более тяжёлой стадии.
    /// </summary>
    [DataField]
    public List<AddictionSymptom> Symptoms = new();

    /// <summary>
    /// Эмоции, которые ломка выдаёт на этой стадии (кашель и прочее).
    /// </summary>
    [DataField]
    public List<AddictionEmote> Emotes = new();
}

/// <summary>
/// Эмоция во время ломки: по поп-апам с шансом, по своему интервалу, или и то и то.
/// </summary>
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
    /// Null - только по поп-апам.
    /// </summary>
    [DataField]
    public TimeSpan? Interval;
}
