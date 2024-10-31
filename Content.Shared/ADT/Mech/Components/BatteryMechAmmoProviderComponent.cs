using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.Weapons.Ranged.Components;

/// <summary>
/// Позволяет оружию меха стрелять проджектайлами.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BatteryMechAmmoProviderComponent : MechAmmoProviderComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [DataField("fireCost")]
    [AutoNetworkedField]
    public float ShotCost = 15f;
}
