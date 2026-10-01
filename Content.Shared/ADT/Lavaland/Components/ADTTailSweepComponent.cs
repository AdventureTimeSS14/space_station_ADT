using Content.Shared.Actions;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTTailSweepComponent : Component
{
    [DataField]
    public EntProtoId Action = "ADTActionTailSweep";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public List<ProtoId<SpeciesPrototype>> Species = new() { "Reptilian", "ADTAshWalkerSpecies", "ADTAshWalkerShamanSpecies" };

    [DataField]
    public float Range = 1.5f;

    [DataField]
    public float StaminaDamage = 24f;

    [DataField]
    public float SelfStaminaDamage = 5f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Weapons/slash.ogg");
}

public sealed partial class ADTTailSweepActionEvent : InstantActionEvent;
