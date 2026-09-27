using Robust.Shared.Audio;
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
    public float SelfDestructTotalIntensity = 2f;

    [DataField, AutoNetworkedField]
    public float SelfDestructSlope = 5f;

    [DataField, AutoNetworkedField]
    public float SelfDestructMaxTileIntensity = 2f;

    [DataField, AutoNetworkedField]
    public bool ForceReplace;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? FailSound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

    [DataField, AutoNetworkedField]
    public string FailMessage = "firing-pin-fail";

    [DataField, AutoNetworkedField]
    public EntityUid? LinkedUser;
}