using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTMechComponent : Component
{
    [DataField]
    public float PilotDamageMultiplier;

    [DataField]
    public DamageModifierSet? Modifiers;

    [DataField]
    public float EquipmentDamageShare = 0.35f;

    [DataField, AutoNetworkedField]
    public bool LightsOn;

    [DataField, AutoNetworkedField]
    public bool Sealed;

    [DataField]
    public SoundSpecifier AccessDeniedSound = new SoundPathSpecifier("/Audio/Machines/Nuke/angry_beep.ogg");

    [DataField]
    public SoundSpecifier EntrySound = new SoundPathSpecifier("/Audio/ADT/Mecha/nominal.ogg");

    [DataField]
    public SoundSpecifier LightsOnSound = new SoundPathSpecifier("/Audio/ADT/Mecha/mechlignton.ogg");

    [DataField]
    public SoundSpecifier LightsOffSound = new SoundPathSpecifier("/Audio/ADT/Mecha/mechlightoff.ogg");

    [DataField]
    public SoundSpecifier SealSound = new SoundPathSpecifier("/Audio/Mecha/sound_mecha_hydraulic.ogg");

    [DataField]
    public SoundSpecifier EquipmentBrokenSound = new SoundPathSpecifier("/Audio/ADT/Mecha/weapdestr.ogg");

    [DataField]
    public EntProtoId LightsAction = "ActionMechTurnLights";

    [DataField]
    public EntProtoId SealAction = "ActionMechInhale";

    [DataField]
    public EntProtoId RadialAction = "ADTActionMechEquipmentRadial";

    [DataField]
    public EntProtoId ReloadAction = "ADTActionMechReload";

    [DataField, AutoNetworkedField]
    public EntityUid? LightsActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? SealActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? RadialActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? ReloadActionEntity;
}
