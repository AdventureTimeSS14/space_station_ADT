using Content.Shared.Roles;
using Robust.Shared.Prototypes;
namespace Content.Server.ADT.SponsorLoadout;

[Prototype("sponsorPersonalLoadout")]
public sealed partial class SponsorPersonalLoadoutPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;

    [DataField]
    public string UserName { get; private set; } = default!;

    [DataField(required: true)]
    public ProtoId<StartingGearPrototype> Equipment;

    [DataField("whitelistJobs")]
    public List<ProtoId<JobPrototype>>? WhitelistJobs { get; private set;}

    [DataField("blacklistJobs")]
    public List<ProtoId<JobPrototype>>? BlacklistJobs { get; private set; }

    [DataField("speciesRestriction")]
    public List<string>? SpeciesRestrictions { get; private set; }

    // Дата истечения лоадаута
    [DataField("expirationDate")]
    public DateTime? ExpirationDate { get; private set; } = null;
}
