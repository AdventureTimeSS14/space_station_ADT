using Content.Shared.Damage;

namespace Content.Shared.ADT.Segmented;

[ByRefEvent]
public readonly record struct ADTSegmentsSpawnedEvent;

[ByRefEvent]
public record struct ADTSegmentDamageForwardEvent(EntityUid Root, DamageSpecifier Damage, float Modifier, EntityUid? Origin, bool Cancelled = false);

[ByRefEvent]
public readonly record struct ADTSegmentStepStartedEvent(EntityUid Root);

[ByRefEvent]
public readonly record struct ADTSegmentLandedEvent(EntityUid Root);

[ByRefEvent]
public readonly record struct ADTRootSegmentLandedEvent(EntityUid Segment);
