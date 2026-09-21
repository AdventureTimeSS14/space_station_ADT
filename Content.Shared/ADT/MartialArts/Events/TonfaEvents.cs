using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MartialArts;

[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class BaseTonfaEvent : EntityEventArgs
{
    [DataField]
    public virtual SoundSpecifier Sound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg");
}

[DataDefinition]
public sealed partial class TonfaSolarPlexusPerformedEvent : BaseTonfaEvent;

[DataDefinition]
public sealed partial class TonfaRibsPerformedEvent : BaseTonfaEvent;

[DataDefinition]
public sealed partial class TonfaWristPerformedEvent : BaseTonfaEvent;

[DataDefinition]
public sealed partial class TonfaLegSweepPerformedEvent : BaseTonfaEvent
{
    [DataField]
    public TimeSpan SlowdownTime = TimeSpan.FromSeconds(10);

    [DataField]
    public float WalkSpeedModifier = 0.6f;

    [DataField]
    public float SprintSpeedModifier = 0.6f;
}

[DataDefinition]
public sealed partial class TonfaGrabBreakPerformedEvent : BaseTonfaEvent;
