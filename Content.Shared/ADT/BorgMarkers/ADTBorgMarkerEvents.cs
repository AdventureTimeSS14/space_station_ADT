using Content.Shared.Actions;

namespace Content.Shared.ADT.BorgMarkers;

[DataDefinition]
public sealed partial class ADTPlaceBorgMarkerEvent : WorldTargetActionEvent
{
}

[DataDefinition]
public sealed partial class ADTCycleBorgMarkerColorEvent : InstantActionEvent
{
}

[DataDefinition]
public sealed partial class ADTClearBorgMarkersEvent : InstantActionEvent
{
}
