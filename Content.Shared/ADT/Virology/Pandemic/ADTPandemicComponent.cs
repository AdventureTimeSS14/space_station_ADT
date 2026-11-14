using Content.Shared.ADT.Virology.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Virology.Pandemic;

[RegisterComponent]
public sealed partial class ADTPandemicComponent : Component
{
    [DataField]
    public string BeakerSlotId = "beakerSlot";

    [DataField]
    public TimeSpan CultureCooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan VaccineCooldown = TimeSpan.FromSeconds(20);

    [ViewVariables]
    public TimeSpan BusyUntil;

    [ViewVariables]
    public ADTDisease? PendingCulture;

    [ViewVariables]
    public string? PendingCultureName;

    [ViewVariables]
    public string? PendingVaccine;

    [ViewVariables]
    public string? PendingVaccineName;

    [DataField]
    public EntProtoId CultureBottle = "ADTVirusCultureBottle";

    [DataField]
    public EntProtoId VaccineBottle = "ADTVaccineBottle";

    [DataField]
    public EntProtoId FormPaper = "Paper";

    [DataField]
    public SoundSpecifier? ReadySound = new SoundPathSpecifier("/Audio/Machines/chime.ogg");

    [DataField]
    public SoundSpecifier? PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    [DataField]
    public SoundSpecifier? WorkingSound = new SoundPathSpecifier("/Audio/Machines/spinning.ogg");

    [ViewVariables]
    public Entity<AudioComponent>? WorkingSoundEntity;

    [DataField]
    public int SymptomChoices = 3;

    [DataField]
    public int MaxSymptomLevel = 4;

    [ViewVariables]
    public List<ProtoId<ADTSymptomPrototype>> Candidates = new();

    [ViewVariables]
    public int CandidatesForDisease = -1;
}
