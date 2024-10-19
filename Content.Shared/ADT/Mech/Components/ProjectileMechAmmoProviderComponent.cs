using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.Weapons.Ranged.Components;

/// <summary>
/// Позволяет оружию меха стрелять проджектайлами.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ProjectileMechAmmoProviderComponent : MechAmmoProviderComponent
{
    [ViewVariables(VVAccess.ReadWrite), DataField("proto", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Prototype = default!;

    [DataField]
    [AutoNetworkedField]
    public int Shots = 30;

    [DataField]
    public int Capacity = 30;

    [DataField]
    public float ReloadTime = 10f;

    [ViewVariables]
    public TimeSpan ReloadEnd = TimeSpan.Zero;

    [ViewVariables]
    public bool Reloading = false;

    [DataField]
    public float ReloadCost = 120f;
}
