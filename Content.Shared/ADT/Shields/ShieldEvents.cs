using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Shared.ADT.Shields;

[ByRefEvent]
public readonly record struct ShieldEnergyFailureEvent(EntityUid Generator);

[ByRefEvent]
public readonly record struct ShieldSegmentRemovedEvent(EntityUid Grid, Vector2i Tile);