using Robust.Shared.Utility;

namespace Content.Server.ADT.Lavaland.Dungeon;

[RegisterComponent, Access(typeof(ADTDungeonGatewaySystem))]
public sealed partial class ADTDungeonGatewayComponent : Component
{
    [DataField(required: true)]
    public ResPath Map;

    [DataField(required: true)]
    public string Key = string.Empty;

    [DataField]
    public bool CopyAtmosphere = true;

    [ViewVariables]
    public bool Linked;
}

[RegisterComponent, Access(typeof(ADTDungeonGatewaySystem))]
public sealed partial class ADTDungeonGatewayExitComponent : Component
{
    [DataField(required: true)]
    public string Key = string.Empty;
}
