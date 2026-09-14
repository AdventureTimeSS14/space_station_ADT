using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Shuttles.Components;

[ByRefEvent]
public record struct DropPodWarSyncEvent(TimeSpan? WarDeclaredTime);
