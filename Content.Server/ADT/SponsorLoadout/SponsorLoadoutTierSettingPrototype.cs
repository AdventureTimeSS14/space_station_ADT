using Content.Shared.Roles;
using Robust.Shared.Prototypes;
namespace Content.Server.ADT.SponsorLoadout;

[Prototype("sponsorLoadoutTierSetting")]
public sealed partial class SponsorLoadoutTierSettingPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("tiers", required: true)]
    public Dictionary<int, ProtoId<StartingGearPrototype>> Tiers { get; private set;} = new();
}
