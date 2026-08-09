using Content.Shared.Chemistry.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Chemistry.Components;

[RegisterComponent]
public sealed partial class ADTLowPressureInjectorComponent : Component
{
    [DataField]
    public float MinPressure = 5f;

    [DataField]
    public float MaxPressure = 50f;

    [DataField(required: true)]
    public ProtoId<InjectorModePrototype> LowPressureMode;

    [DataField(required: true)]
    public ProtoId<InjectorModePrototype> NormalMode;

    [DataField]
    public bool InLowPressure;
}
