using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor.ThermalCloak;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ThermalCloakComponent : Component
{
    public bool Enabled;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan ForcedCooldown = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public float Opacity = 0.1f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? CloakSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? UncloakSound;

    [DataField, AutoNetworkedField]
    public bool RestrictWeapons;

    /// <summary>
    /// Layers to hide while cloaked
    /// </summary>
    [DataField]
    public HashSet<HumanoidVisualLayers> CloakedHideLayers = new();

    /// <summary>
    /// Amount of time after uncloaking weapons remain locked
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public TimeSpan UncloakWeaponLock = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "ADTActionToggleCloak";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public EntProtoId CloakEffect = "RMCEffectCloak";

    [DataField, AutoNetworkedField]
    public EntProtoId UncloakEffect = "RMCEffectUncloak";

    [DataField, AutoNetworkedField]
    public bool NinjaSuit = false;
}
