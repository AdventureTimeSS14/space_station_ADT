using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Rituals;

[Prototype("adtRitual")]
public sealed partial class ADTRitualPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<string> Categories = new();

    [DataField(required: true)]
    public LocId Name = default!;

    [DataField]
    public LocId? Description;

    [DataField]
    public LocId? DyeFluff;

    [DataField]
    public int ExtraInvokers;

    [DataField]
    public int ExtraShamanInvokers;

    [DataField]
    public bool ShamanOnly;

    [DataField]
    public List<ProtoId<SpeciesPrototype>> AllowedSpecies = new();

    [DataField]
    public List<ADTRitualThing> RequiredThings = new();

    [DataField]
    public float FindingRange = 2f;

    [DataField]
    public int Charges = -1;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(100);

    [DataField]
    public TimeSpan CastTime;

    [DataField]
    public float FailChance = 0.1f;

    [DataField]
    public float DisasterChance = 0.1f;

    [DataField]
    public string? NeededDye;

    [DataField]
    public string? TotemDye;

    [DataField]
    public bool DeleteThingsOnSuccess = true;

    [DataField]
    public bool DeleteThingsOnFail;

    [DataField(serverOnly: true)]
    public List<ADTRitualCheck> Checks = new();

    [DataField(serverOnly: true)]
    public List<ADTRitualModifier> Modifiers = new();

    [DataField(serverOnly: true)]
    public List<ADTRitualEffect> Effects = new();

    [DataField(serverOnly: true)]
    public List<ADTRitualEffect> DisasterEffects = new();

    [DataField]
    public SoundSpecifier StartSound = new SoundCollectionSpecifier("ADTRitualStart");

    [DataField]
    public SoundSpecifier SuccessSound = new SoundCollectionSpecifier("ADTRitualSuccess");

    [DataField]
    public SoundSpecifier FailSound = new SoundCollectionSpecifier("ADTRitualFail");
}

[DataDefinition]
public sealed partial class ADTRitualThing
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;

    [DataField]
    public int Amount = 1;

    [DataField]
    public bool Consume = true;

    [DataField(required: true)]
    public LocId Name = default!;
}
