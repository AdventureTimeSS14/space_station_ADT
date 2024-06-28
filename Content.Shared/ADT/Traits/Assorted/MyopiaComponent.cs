using Robust.Shared.GameStates;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This is used for making something blind forever.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MyopiaComponent : Component
{
    [DataField]
    public int EyeDamage = 3;

    public bool Active = true;
}

