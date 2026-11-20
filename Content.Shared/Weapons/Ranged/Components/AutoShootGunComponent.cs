using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Allows GunSystem to automatically fire while this component is enabled
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedGunSystem)), AutoGenerateComponentState]
public sealed partial class AutoShootGunComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Enabled;

    // ADT-Tweak-Start
    /// <summary>
    /// Mono - For how much more time to keep shooting, ignores whether this component is enabled.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RemainingTime = TimeSpan.Zero;
    // ADT-Tweak-End
}
