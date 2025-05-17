using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Supermatter.Components;

/// <summary>
/// An entity with this component will not be able to launch supermatter.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SupermatterIgnoreComponent : Component
{
}

[RegisterComponent]
public sealed partial class SupermatterSootherComponent : Component
{
}

/// <summary>
/// Immune for hallucination
/// </summary>
[RegisterComponent]
public sealed partial class SupermatterHallucinationImmuneComponent : Component
{
}

/// <summary>
/// Supermatter Immune component
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SupermatterImmuneComponent : Component
{
}

/// <summary>
/// Supermatter Food Component
/// </summary>
[RegisterComponent]
public sealed partial class SupermatterFoodComponent : Component
{
    [DataField]
    public int Energy { get; set; } = 1;
}

/// <summary>
/// Supermatter OnInert Core Component
/// </summary>
[RegisterComponent]
public sealed partial class SupermatterNobliumCoreComponent : Component
{
}

