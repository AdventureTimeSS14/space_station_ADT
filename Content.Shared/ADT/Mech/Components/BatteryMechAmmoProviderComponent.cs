using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
namespace Content.Shared.ADT.Weapons.Ranged.Components;

/// <summary>
/// Позволяет оружию меха стрелять проджектайлами.
/// Использует батарею меха
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BatteryMechAmmoProviderComponent : MechAmmoProviderComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true)]
    public EntProtoId Prototype = default!;

    [DataField("fireCost")]
    [AutoNetworkedField]
    public float ShotCost = 15f;
}
