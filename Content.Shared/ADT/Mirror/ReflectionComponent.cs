using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mirror;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MirrorReflectionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool ReflectIfInvisible = false;

    [ViewVariables]
    public bool Active = true;
}
