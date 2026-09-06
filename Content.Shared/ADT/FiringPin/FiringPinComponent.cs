using Content.Shared.Access;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.FiringPin;

public enum FiringPinType : byte
{
    None,
    TestRange,
    Implant,
    Loyalty,
    DNA,
    Clown,
    Holy,
    Tag,
    Access,
    SecLevel,
    Explorer,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FiringPinComponent : Component
{
    [DataField, AutoNetworkedField]
    public FiringPinType PinType = FiringPinType.None;

    [DataField, AutoNetworkedField]
    public bool SelfDestruct;

    [DataField, AutoNetworkedField]
    public bool ForceReplace;

    [DataField, AutoNetworkedField]
    public string FailMessage = "firing-pin-fail";

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedUser;

    [DataField, AutoNetworkedField]
    public EntProtoId? RequiredImplant;

    [DataField, AutoNetworkedField]
    public List<ProtoId<AccessLevelPrototype>> RequiredAccess = new();

    [DataField, AutoNetworkedField]
    public bool PassForClowns;

    [DataField, AutoNetworkedField]
    public ProtoId<TagPrototype>? RequiredSuitTag;

    [DataField, AutoNetworkedField]
    public List<string> AllowedAlertLevels = new();
}