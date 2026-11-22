using Content.Shared.Access;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.FiringPin;

[DataDefinition, Serializable, NetSerializable]
public sealed partial class FiringPinCheck
{
    [DataField]
    public FiringPinType Type = FiringPinType.None;

    [DataField]
    public EntProtoId? RequiredImplant;

    [DataField]
    public List<ProtoId<AccessLevelPrototype>> RequiredAccess = new();

    [DataField]
    public bool PassForClowns;

    [DataField]
    public bool PassForFakeMindShield;

    [DataField]
    public ProtoId<TagPrototype>? RequiredSuitTag;

    [DataField]
    public List<string> AllowedAlertLevels = new();

    [DataField]
    public string? SelectedAlertLevel;

    [DataField]
    public EntityWhitelist? RequiredWhitelist;
}