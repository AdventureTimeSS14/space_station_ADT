// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Состояние одной зависимости у моба: настройки лежат в AddictionPrototype.
/// </summary>
[DataDefinition]
public sealed partial class AddictionChannel
{
    [DataField("kind", required: true)]
    public ProtoId<AddictionPrototype> Kind;

    /// <summary>
    /// Уровень привыкания, 0..AddictionPrototype.MaxLevel.
    /// </summary>
    [DataField]
    public float Level;

    /// <summary>
    /// Неизлечимая зависимость (даётся трайтом): уровень не спадает, детоксин не помогает.
    /// </summary>
    [DataField]
    public bool Permanent;

    /// <summary>
    /// Каналы, добавленные в рантайме (трайт-эффект, передоз), приходят без времени дозы:
    /// без этого флага ломка началась бы мгновенно.
    /// </summary>
    [DataField]
    public bool Initialized;

    [DataField]
    public TimeSpan LastDoseTime;

    [DataField]
    public TimeSpan NextPopupTime;

    /// <summary>
    /// Порог уже был превышен: для одноразовых поп-апов подсадки и выздоровления.
    /// </summary>
    [DataField]
    public bool WasAddicted;

    [DataField]
    public bool InWithdrawal;

    [DataField]
    public bool CravingWarned;

    /// <summary>
    /// Стадия ломки, 0 - ломки нет.
    /// </summary>
    public int Stage;

    /// <summary>
    /// Индекс прошлого поп-апа: соседние фразы не повторяются.
    /// </summary>
    public int LastPopupIndex = -1;

    /// <summary>
    /// Время следующей эмоции по индексу в AddictionStageData.Emotes текущей стадии.
    /// </summary>
    public List<TimeSpan> NextEmoteTimes = new();
}
