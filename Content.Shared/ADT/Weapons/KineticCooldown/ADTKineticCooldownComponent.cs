using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Weapons.KineticCooldown;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class ADTKineticCooldownComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUse;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan LastUseStart;

    [DataField, AutoNetworkedField]
    public float BaseRangedMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float BaseMeleeMultiplier = 1f;

    [DataField, AutoNetworkedField]
    public float RangedMultiplierModifier = 1f;

    [DataField, AutoNetworkedField]
    public float MeleeMultiplierModifier = 1f;

    [ViewVariables, AutoNetworkedField]
    public float UpgradeRangedMultiplier = 1f;

    [ViewVariables, AutoNetworkedField]
    public float UpgradeMeleeMultiplier = 1f;

    [ViewVariables]
    public float CurrentRangedMultiplier => BaseRangedMultiplier * RangedMultiplierModifier * UpgradeRangedMultiplier;

    [ViewVariables]
    public float CurrentMeleeMultiplier => BaseMeleeMultiplier * MeleeMultiplierModifier * UpgradeMeleeMultiplier;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? RechargeSound;

    [ViewVariables, AutoNetworkedField]
    public bool RechargePending;
}
