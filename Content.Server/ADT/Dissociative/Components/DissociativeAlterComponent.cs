namespace Content.Server.ADT.Dissociative.Components;

[RegisterComponent]
public sealed partial class DissociativeAlterComponent : Component
{
    [ViewVariables]
    public EntityUid Host;

    [ViewVariables]
    public bool Manifested;

    [ViewVariables]
    public bool SetupDone;

    [ViewVariables]
    public bool AntagRolled;

    [ViewVariables]
    public EntityUid? ManifestActionEntity;

    [ViewVariables]
    public EntityUid? FrontActionEntity;

    [ViewVariables]
    public EntityUid? SetupActionEntity;

    [ViewVariables]
    public List<EntityUid> Clothing = new();
}
