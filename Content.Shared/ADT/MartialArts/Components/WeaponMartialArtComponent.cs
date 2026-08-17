using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.MartialArts;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeaponMartialArtComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ComboListPrototype> Combos;

    [DataField(required: true)]
    public MartialArtsForms MartialArtsForm;

    [DataField]
    public TimeSpan ComboWindow = TimeSpan.FromSeconds(5);

    [DataField]
    public int LastAttacksLimit = 4;

    [DataField]
    public bool RequireSameTarget = true;

    [DataField]
    public bool BlockedByKnownMartialArt = true;

    [DataField]
    public bool ResetPopup = true;

    [DataField]
    public TimeSpan StepCooldown = TimeSpan.FromSeconds(1);

    [DataField]
    public List<ComboAttackType> ThrottledSteps = new() { ComboAttackType.Help, ComboAttackType.Grab };

    [DataField, AutoNetworkedField]
    public TimeSpan NextThrottledStep;

    [ViewVariables]
    public List<ComboPrototype> AllowedCombos = new();

    [DataField, AutoNetworkedField]
    public List<ComboAttackType> LastAttacks = new();

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentTarget;

    [DataField, AutoNetworkedField]
    public EntityUid? CurrentUser;

    [DataField, AutoNetworkedField]
    public TimeSpan ResetTime;

    [DataField, AutoNetworkedField]
    public ProtoId<ComboPrototype>? BeingPerformed;
}
