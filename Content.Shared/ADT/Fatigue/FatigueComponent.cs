// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Fatigue;

/// <summary>
/// Биологическая потребность в отдыхе. Компонент только у органических видов.
/// Стадии: 0 - бодр, 1 - лёгкая, 2 - средняя, 3 - тяжёлая, 4 - коллапс (принудительный сон).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedFatigueSystem))]
public sealed partial class FatigueComponent : Component
{
    /// <summary>Текущая стадия усталости: 0 = бодр, 4 = коллапс-сон.</summary>
    [DataField, AutoNetworkedField]
    public int Stage;

    /// <summary>Момент перехода на следующую стадию (или пробуждения от коллапса).</summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextStageAt;

    /// <summary>Момент следующей зевоты (стадия 1+).</summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextYawnAt;

    /// <summary>Момент следующего спотыкания (стадия 3+).</summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextStumbleAt;

    /// <summary>Накопленное время добровольного сна для снижения стадии.</summary>
    [DataField]
    public TimeSpan SleepRecoveryAccumulated;

    /// <summary>Истина, пока персонаж спит принудительно (коллапс на стадии 4).</summary>
    [DataField, AutoNetworkedField]
    public bool FatigueForcedSleep;

    /// <summary>HUD-алерт усталости, иконка зависит от стадии (fatigue1-4).</summary>
    [DataField]
    public ProtoId<AlertPrototype> Alert = "Fatigue";

    /// <summary>Минимальное время бодрствования до первой стадии.</summary>
    [DataField]
    public TimeSpan MinAlertDuration = TimeSpan.FromMinutes(15);

    /// <summary>Максимальное время бодрствования до первой стадии.</summary>
    [DataField]
    public TimeSpan MaxAlertDuration = TimeSpan.FromMinutes(60);

    /// <summary>Длительность стадии 1 (лёгкая).</summary>
    [DataField]
    public TimeSpan Stage1Duration = TimeSpan.FromMinutes(10);

    /// <summary>Длительность стадии 2 (средняя).</summary>
    [DataField]
    public TimeSpan Stage2Duration = TimeSpan.FromMinutes(10);

    /// <summary>Длительность стадии 3 (тяжёлая).</summary>
    [DataField]
    public TimeSpan Stage3Duration = TimeSpan.FromMinutes(5);

    /// <summary>Длительность принудительного сна при коллапсе до естественного пробуждения.</summary>
    [DataField]
    public TimeSpan CollapseSleepDuration = TimeSpan.FromMinutes(3);

    /// <summary>Время добровольного сна для снижения одной стадии.</summary>
    [DataField]
    public TimeSpan SleepRecoveryPerStage = TimeSpan.FromSeconds(90);

    /// <summary>Множитель скорости на стадии 1.</summary>
    [DataField]
    public float Stage1SpeedModifier = 0.9f;

    /// <summary>Множитель скорости на стадии 2.</summary>
    [DataField]
    public float Stage2SpeedModifier = 0.8f;

    /// <summary>Множитель скорости на стадии 3 и выше.</summary>
    [DataField]
    public float Stage3SpeedModifier = 0.4f;

    /// <summary>Сила размытия зрения на стадии 3+.</summary>
    [DataField]
    public float Stage3Blur = 3.5f;

    /// <summary>Минимальный интервал зевоты на стадии 1 (секунды).</summary>
    [DataField]
    public float Stage1YawnMin = 150f;

    /// <summary>Максимальный интервал зевоты на стадии 1 (секунды).</summary>
    [DataField]
    public float Stage1YawnMax = 180f;

    /// <summary>Минимальный интервал зевоты на стадии 2 (секунды).</summary>
    [DataField]
    public float Stage2YawnMin = 45f;

    /// <summary>Максимальный интервал зевоты на стадии 2 (секунды).</summary>
    [DataField]
    public float Stage2YawnMax = 60f;

    /// <summary>Минимальный интервал зевоты на стадии 3 (секунды).</summary>
    [DataField]
    public float Stage3YawnMin = 20f;

    /// <summary>Максимальный интервал зевоты на стадии 3 (секунды).</summary>
    [DataField]
    public float Stage3YawnMax = 35f;

    /// <summary>Минимальный интервал спотыкания на стадии 3 (секунды).</summary>
    [DataField]
    public float Stage3StumbleMin = 40f;

    /// <summary>Максимальный интервал спотыкания на стадии 3 (секунды).</summary>
    [DataField]
    public float Stage3StumbleMax = 90f;
}
