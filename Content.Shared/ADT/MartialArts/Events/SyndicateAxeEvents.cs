using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.MartialArts;

[Serializable, NetSerializable, ImplicitDataDefinitionForInheritors]
public abstract partial class BaseSyndicateAxeEvent : EntityEventArgs
{
    [DataField]
    public virtual SoundSpecifier Sound { get; set; } = new SoundPathSpecifier("/Audio/Effects/hit_kick.ogg");
}

[DataDefinition]
public sealed partial class SyndicateAxeKnockdownPerformedEvent : BaseSyndicateAxeEvent;

[DataDefinition]
public sealed partial class SyndicateAxeGrabPushPerformedEvent : BaseSyndicateAxeEvent
{
    [DataField]
    public TimeSpan SlowdownTime = TimeSpan.FromSeconds(3);

    [DataField]
    public float WalkSpeedModifier = 0.6f;

    [DataField]
    public float SprintSpeedModifier = 0.6f;
}
