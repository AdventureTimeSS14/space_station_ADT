using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.ADT.SponsorLoadout;

[Prototype("sponsorLoadoutTierSetting")]
public sealed partial class SponsorLoadoutTierSettingPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("tiers", required: true)]
    public Dictionary<int, ProtoId<StartingGearPrototype>> Tiers { get; private set;} = new();
}
