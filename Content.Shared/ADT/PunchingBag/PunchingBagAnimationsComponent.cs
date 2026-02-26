using Robust.Shared.GameStates;

namespace Content.Shared.ADT.PunchingBag;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PunchingBagAnimationsComponent : Component
{
    [DataField, AutoNetworkedField]
    public string AnimationState = "swinging";
}

