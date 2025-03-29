using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Traits;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class FrailComponent : Component
{
    [DataField]
    public Dictionary<string, float> Modifiers = new();
}
