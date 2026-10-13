// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Состояние одной зависимости у моба. Настроек тут нет - только то, что меняется в игре.
/// </summary>
[DataDefinition]
public sealed partial class AddictionChannel
{
    /// <summary>
    /// Какая это зависимость. В yml пишется как kind.
    /// </summary>
    [DataField("kind", required: true)]
    public ProtoId<AddictionPrototype> Kind;

    /// <summary>
    /// Текущий уровень привыкания, 0..AddictionPrototype.MaxLevel.
    /// </summary>
    [DataField]
    public float Level;

    /// <summary>
    /// Неизлечимая зависимость (даётся трайтом): уровень не спадает, детоксин не помогает.
    /// </summary>
    [DataField]
    public bool Permanent;

    /// <summary>
    /// Канал уже прошёл первичную настройку в AddictionSystem.
    /// Каналы, добавленные в рантайме (трайт-эффект, передоз), приходят без времени дозы:
    /// без этого флага ломка началась бы мгновенно.
    /// </summary>
    [DataField]
    public bool Initialized;

    /// <summary>
    /// Время последней дозы по игровому таймеру.
    /// </summary>
    [DataField]
    public TimeSpan LastDoseTime;

    /// <summary>
    /// Время следующего поп-апа ломки.
    /// </summary>
    [DataField]
    public TimeSpan NextPopupTime;

    /// <summary>
    /// Был ли превышен порог зависимости: для одноразовых поп-апов подсадки и выздоровления.
    /// </summary>
    [DataField]
    public bool WasAddicted;

    /// <summary>
    /// Идёт ли сейчас ломка. Нужно, чтобы не спамить поп-ап дозы на каждом цикле метаболизма.
    /// </summary>
    [DataField]
    public bool InWithdrawal;

    /// <summary>
    /// Предупреждение о нарастающей тяге уже показано в этом цикле без дозы.
    /// </summary>
    [DataField]
    public bool CravingWarned;

    /// <summary>
    /// Текущая стадия ломки, 0 - ломки нет. Обновляется AddictionSystem.
    /// </summary>
    public int Stage;

    /// <summary>
    /// Индекс прошлого поп-апа, чтобы соседние фразы не повторялись.
    /// </summary>
    public int LastPopupIndex = -1;

    /// <summary>
    /// Время следующей эмоции по индексу в AddictionStageData.Emotes текущей стадии.
    /// Пересобирается при смене стадии.
    /// </summary>
    public List<TimeSpan> NextEmoteTimes = new();
}
