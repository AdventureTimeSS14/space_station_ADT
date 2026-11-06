// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Состояние одной зависимости у моба.
/// </summary>
[DataDefinition]
public sealed partial class AddictionChannel
{
    [DataField("kind", required: true)]
    public ProtoId<AddictionPrototype> Kind;

    [DataField]
    public float Level;

    /// <summary>
    /// Уровень не спадает, детоксин не помогает.
    /// </summary>
    [DataField]
    public bool Permanent;

    [DataField]
    public bool Initialized;

    [DataField]
    public TimeSpan LastDoseTime;

    [DataField]
    public TimeSpan NextPopupTime;

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

    public int LastPopupIndex = -1;

    /// <summary>
    /// По индексу в AddictionStageData.Emotes.
    /// </summary>
    public List<TimeSpan> NextEmoteTimes = new();
}
