using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Chemistry.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTInjectionSlowdownComponent : Component
{
    [DataField]
    public bool RequiresHelmet;
}
