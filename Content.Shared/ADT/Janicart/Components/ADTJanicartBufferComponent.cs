using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Janicart.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedADTJanicartSystem))]
public sealed partial class ADTJanicartBufferComponent : Component
{
    [DataField]
    public float Range = 2f;

    [DataField]
    public EntProtoId MoppedEffect = "PuddleSparkle";

    [DataField]
    public SoundSpecifier WashSound = new SoundPathSpecifier("/Audio/Effects/Fluids/watersplash.ogg");

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextCheck = TimeSpan.Zero;

    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(0.5);
}