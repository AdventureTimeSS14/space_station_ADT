using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.Shared.HunterEye;

[RegisterComponent, NetworkedComponent, Access(typeof(HunterEyeDamageReductionSystem))]
public sealed partial class HunterEyeDamageReductionComponent : Component
{

}
