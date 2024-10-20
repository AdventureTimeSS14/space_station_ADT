using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.Weapons.Ranged.Components;

/// <summary>
/// Позволяет оружию меха стрелять хитсканом.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HitscanMechAmmoProviderComponent : MechAmmoProviderComponent
{
    [DataField("fireCost")]
    [AutoNetworkedField]
    public float ShotCost = 15f;

    [DataField(required: true)]
    public ProtoId<HitscanPrototype> Proto;
}
