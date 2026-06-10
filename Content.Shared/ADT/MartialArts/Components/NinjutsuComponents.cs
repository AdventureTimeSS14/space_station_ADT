using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Alert;

namespace Content.Shared.ADT.MartialArts;

[RegisterComponent, NetworkedComponent]
public sealed partial class NinjutsuSneakAttackComponent : Component
{
    [DataField]
    public float Multiplier = 2f;

    [DataField]
    public float AssassinateModifier = 180f;

    [DataField]
    public float AssassinateUnarmedModifier = 115f;

    [DataField]
    public float AssassinateArmorPierce = -2.5f;

    [DataField]
    public float TakedownSlowdownTime = 5f;

    [DataField]
    public float TakedownMuteTime = 10f;

    [DataField]
    public float TakedownSpeedModifier = 0.2f;

    [DataField]
    public float TakedownBackstabMultiplier = 1.5f;

    [DataField]
    public SoundSpecifier AssassinateSoundUnarmed = new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg");

    [DataField]
    public SoundSpecifier AssassinateSoundArmed = new SoundPathSpecifier("/Audio/Weapons/genhit2.ogg");

    [DataField]
    public string AssassinateComboName = "Assassinate";

    [DataField]
    public string TakedownComboName = "Ninjutsu Takedown";

    [DataField]
    public ProtoId<AlertPrototype> Alert = "SneakAttack";
}

[RegisterComponent, NetworkedComponent]
public sealed partial class NinjutsuLossOfSurpriseComponent : Component;
