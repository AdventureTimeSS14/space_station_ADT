using Content.Server.ADT.Shuttles.Systems;

namespace Content.Server.ADT.Shuttles.Components;

[RegisterComponent, Access(typeof(ADTHandheldShuttleConsoleSystem))]
public sealed partial class ADTHandheldShuttleConsoleComponent : Component
{
    [ViewVariables]
    public EntityUid? LinkedConsole;

    [ViewVariables]
    public Dictionary<EntityUid, EntityUid> Viewers = new();
}
