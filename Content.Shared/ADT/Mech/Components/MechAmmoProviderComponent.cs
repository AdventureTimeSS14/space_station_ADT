using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.Weapons.Ranged.Components;

[NetworkedComponent]
public abstract partial class MechAmmoProviderComponent : AmmoProviderComponent
{
}
