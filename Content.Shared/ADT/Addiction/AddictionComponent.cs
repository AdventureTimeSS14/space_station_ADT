// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Компонент зависимости от реагентов (алкоголь, никотин, наркотики, лекарства).
/// Появляется при первом употреблении, через трайт (стартовая зависимость) или при передозе лекарств.
/// </summary>
// Каналы меняют серверные системы (AddictionSystem, AdjustAddictionLevelEffectSystem)
// и Shared-эффекты трайтов (AddictionTraitEffect, RandomAddictionEffect), поэтому
// [Access(typeof(...))] осознанно не ставится: эффекты не являются системами.
[RegisterComponent]
public sealed partial class AddictionComponent : Component
{
    /// <summary>
    /// Каналы зависимости. Каждый канал живёт своей жизнью: свой уровень, своя ломка.
    /// </summary>
    [DataField]
    public List<AddictionChannel> Channels = new();

    /// <summary>
    /// Уровень, ниже которого зависимость считается вылеченной (стадия 0).
    /// Стадии: 1 - лёгкая (25+), 2 - средняя (50+), 3 - тяжёлая (75+), см. AddictionStage.
    /// </summary>
    [DataField]
    public float Threshold = AddictionStage.MildThreshold;

    /// <summary>
    /// Спад уровня в секунду. 100 за ~100 минут воздержания: при стартовом уровне 100
    /// порог (50) достигается через ~50 минут, тяжёлая стадия ломки (с 30-й минуты) успевает наступить.
    /// </summary>
    [DataField]
    public float DecayRate = 100f / 6000f;

    /// <summary>
    /// Рост уровня за цикл метаболизма реагента (примерно раз в секунду,
    /// пока реагент есть в крови).
    /// </summary>
    [DataField]
    public float GainPerTick = 0.04f;

    /// <summary>
    /// Время без дозы до начала ломки.
    /// </summary>
    [DataField]
    public TimeSpan WithdrawalDelay = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Интервал между поп-апами симптомов ломки.
    /// </summary>
    [DataField]
    public TimeSpan PopupInterval = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Как долго держатся симптомы после последнего продления (дрожь и статус-эффекты).
    /// </summary>
    [DataField]
    public TimeSpan SymptomDuration = TimeSpan.FromSeconds(35);

    /// <summary>
    /// Как часто продлеваются симптомы во время ломки.
    /// </summary>
    [DataField]
    public TimeSpan SymptomRefreshInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Амплитуда дрожи на лёгкой стадии ломки.
    /// </summary>
    [DataField]
    public float MildJitterAmplitude = 6f;

    /// <summary>
    /// Амплитуда дрожи на средней стадии ломки.
    /// </summary>
    [DataField]
    public float MediumJitterAmplitude = 8f;

    /// <summary>
    /// Амплитуда дрожи на тяжёлой стадии ломки.
    /// </summary>
    [DataField]
    public float SevereJitterAmplitude = 10f;

    /// <summary>
    /// Частота дрожи во время ломки.
    /// </summary>
    [DataField]
    public float JitterFrequency = 3f;

    /// <summary>
    /// Статус-эффект косноязычия на средней стадии алкогольной ломки.
    /// </summary>
    [DataField]
    public EntProtoId SlurredEffect = "StatusEffectSlurred";

    /// <summary>
    /// Статус-эффект заикания на средней стадии никотиновой и наркотической ломки.
    /// </summary>
    [DataField]
    public EntProtoId StutterEffect = "StatusEffectStutter";

    /// <summary>
    /// Статус-эффект слабости на тяжёлой стадии ломки.
    /// </summary>
    [DataField]
    public EntProtoId WeaknessEffect = "StatusEffectWithdrawalWeakness";

    /// <summary>
    /// Серый фильтр ломки
    /// </summary>
    public bool WithdrawalMonochromacyApplied;

    /// <summary>
    /// Реагент, который кормит никотиновый канал (по id, у никотина нет своей группы метаболизма).
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> NicotineReagent = "Nicotine";

    /// <summary>
    /// Id родительского прототипа, по которому реагент считается алкоголем (в ADT все спиртные наследуют BaseAlcohol).
    /// </summary>
    [DataField]
    public string AlcoholParentId = "BaseAlcohol";

    /// <summary>
    /// Группа реагента (ReagentPrototype.Group), по которой реагент считается наркотиком.
    /// </summary>
    [DataField]
    public string NarcoticGroup = "Narcotics";

    /// <summary>
    /// Флаг рандомного трайта: при спавне выбрать случайный канал из тех, что ещё не выбраны.
    /// Ставится эффектом трайта RandomAddictionEffect, обрабатывается AddictionSystem.
    /// </summary>
    public bool RandomizeChannel;
}

/// <summary>
/// Тип зависимости. Определяет, какие реагенты кормят канал и какие симптомы у ломки.
/// </summary>
public enum AddictionKind : byte
{
    Alcohol,
    Nicotine,
    Drug,
    Medicine,
    Omnizine,
}

/// <summary>
/// Канал зависимости: состояние одного типа привыкания.
/// </summary>
[DataDefinition]
public sealed partial class AddictionChannel
{
    [DataField(required: true)]
    public AddictionKind Kind;

    /// <summary>
    /// Текущий уровень привыкания 0..100.
    /// </summary>
    [DataField]
    public float Level;

    /// <summary>
    /// Время последней дозы (по игровому таймеру).
    /// </summary>
    [DataField]
    public TimeSpan LastDoseTime;

    /// <summary>
    /// Время следующего поп-апа симптомов.
    /// </summary>
    [DataField]
    public TimeSpan NextPopupTime;

    /// <summary>
    /// Время следующего продления симптомов (чтобы не дёргать DoJitter каждый тик).
    /// </summary>
    [DataField]
    public TimeSpan NextSymptomsTime;

    /// <summary>
    /// Был ли превышен порог зависимости (для одноразовых поп-апов подсадки и выздоровления).
    /// </summary>
    [DataField]
    public bool WasAddicted;

    /// <summary>
    /// Идёт ли сейчас ломка (нужно, чтобы не спамить поп-ап дозы на каждом цикле метаболизма).
    /// </summary>
    [DataField]
    public bool InWithdrawal;

    /// <summary>
    /// Текущая стадия ломки (равна стадии зависимости: 1 - лёгкая, 2 - средняя, 3 - тяжёлая).
    /// Обновляется AddictionSystem.
    /// </summary>
    public int Stage;

    /// <summary>
    /// Неизлечимая зависимость (даётся трайтом): уровень не спадает, детоксин не помогает.
    /// </summary>
    [DataField]
    public bool Permanent;
}
