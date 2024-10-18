using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Ranged.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedGunSystem))]
public sealed partial class TargetedProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Target;

    // ADT Crawling abuse fix
    [DataField, AutoNetworkedField]
    public EntityCoordinates? TargetCoords;
}
