using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.ADT.Silicon;

[RegisterComponent]
public sealed partial class MobIpcComponent : Component
{
    [DataField]
    public bool DisablePointLightOnDeath = false;

    [DataField]
    public bool LightDisabledByDeath;
}
