namespace Content.Server.ADT._Mono.FireControl;

[RegisterComponent]
public sealed partial class FireControlServerComponent : Component
{
    [ViewVariables]
    public EntityUid? ConnectedGrid = null;

    [ViewVariables]
    public HashSet<EntityUid> Controlled = [];

    [ViewVariables]
    public HashSet<EntityUid> Consoles = [];

    [ViewVariables]
    public Dictionary<EntityUid, EntityUid> Leases;

    [ViewVariables, DataField]
    public int ProcessingPower;

    [ViewVariables]
    public int UsedProcessingPower;

    [ViewVariables, DataField]
    public int MaxConsoles = 1;

    [ViewVariables, DataField]
    public bool EnforceMaxConsoles;
}
