using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.ADT.SponsorLoadout;

[Prototype("sponsorPersonalLoadout")]
public sealed class SponsorPersonalLoadoutPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField]
    public string UserName { get; } = default!;

    [DataField(required: true)]
    public ProtoId<StartingGearPrototype> Equipment;

    [DataField("whitelistJobs", customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string>? WhitelistJobs { get; }

    [DataField("blacklistJobs", customTypeSerializer: typeof(PrototypeIdListSerializer<JobPrototype>))]
    public List<string>? BlacklistJobs { get; }

    [DataField("speciesRestriction")]
    public List<string>? SpeciesRestrictions { get; }

    // Дата истечения лоадаута
    [DataField("expirationDate")]
    public DateTime? ExpirationDate { get; } = null;
}
