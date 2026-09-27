using Robust.Shared.GameStates;

namespace Content.Shared.ADT.FiringPin;

public enum FiringPinType : byte
{
    None,
    TestRange,
    Implant,
    DNA,
    Clown,
    Tag,
    Access,
    SecLevel,
    Explorer,
    Component,
}

public enum FiringPinLogic : byte
{
    Any,
    All,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FiringPinComponent : Component
{
    [DataField, AutoNetworkedField]
    public FiringPinLogic Logic = FiringPinLogic.Any;

    [DataField, AutoNetworkedField]
    public List<FiringPinCheck> Checks = new();

    [DataField, AutoNetworkedField]
    public bool SelfDestruct;

    [DataField, AutoNetworkedField]
    public bool ForceReplace;

    [DataField, AutoNetworkedField]
    public string FailMessage = "firing-pin-fail";

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedUser;
}