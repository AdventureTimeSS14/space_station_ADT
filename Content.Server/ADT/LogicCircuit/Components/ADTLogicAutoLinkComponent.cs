namespace Content.Server.ADT.LogicCircuit.Components;

[RegisterComponent]
public sealed partial class ADTLogicAutoLinkComponent : Component
{
    [DataField(required: true)]
    public List<ADTLogicAutoLinkEntry> Links = new();
}

[DataDefinition]
public sealed partial class ADTLogicAutoLinkEntry
{
    [DataField(required: true)]
    public string Channel = string.Empty;

    [DataField(required: true)]
    public string OwnPort = string.Empty;

    [DataField(required: true)]
    public string TargetPort = string.Empty;

    [DataField]
    public bool Outgoing = true;
}
