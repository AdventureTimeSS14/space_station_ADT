using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.ADT.Species.Drask;

/// <summary>
/// Компонент для Drask, позволяющий лечиться при низкой температуре тела.
/// </summary>
[RegisterComponent]
public sealed partial class DraskColdHealingComponent : Component
{
    [DataField("temperatureThreshold")]
    public float TemperatureThreshold = -30f;

    [DataField("healing")]
    public DamageSpecifier Healing = new()
    {
        DamageDict = new()
        {
            { "Blunt", -0.5f }
        }
    };

    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextHealTime;
}
