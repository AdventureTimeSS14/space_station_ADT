using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Fauna.Components;

[RegisterComponent]
public sealed partial class ADTBurrowAwayComponent : Component
{
    [DataField]
    public float TriggerRange = 6f;

    [DataField]
    public TimeSpan ChaseTime = TimeSpan.FromSeconds(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? BurrowAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextScan;

    [DataField]
    public TimeSpan ScanInterval = TimeSpan.FromSeconds(1);

    [DataField]
    public LocId Popup = "adt-goldgrub-burrow";

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Effects/break_stone.ogg");
}
