using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.ADT.SponsorLoadout;

[Prototype("sponsorLoadoutTierSetting")]
public sealed class SponsorLoadoutTierSettingPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("tiers", required: true)]
    public Dictionary<int, ProtoId<StartingGearPrototype>> Tiers { get; } = new();
}
