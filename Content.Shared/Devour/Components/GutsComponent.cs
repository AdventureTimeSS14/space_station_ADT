using Content.Shared.Chemistry.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Devour.Components;

/// <summary>
/// Component that provides a "guts" container for storing digested materials
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GutsComponent : Component
{
    /// <summary>
    /// The container that holds digested materials
    /// </summary>
    public Container GutsContainer = default!;

    /// <summary>
    /// Maximum capacity of the guts container
    /// </summary>
    [DataField("maxCapacity")]
    public int MaxCapacity = 100;

    /// <summary>
    /// Current amount of digested material in the guts
    /// </summary>
    [ViewVariables]
    public int CurrentAmount = 0;

    /// <summary>
    /// Whether the guts are full
    /// </summary>
    [ViewVariables]
    public bool IsFull => CurrentAmount >= MaxCapacity;

    /// <summary>
    /// The solution containing digested nutrients
    /// </summary>
    [ViewVariables]
    public Entity<SolutionComponent>? NutrientSolution;

    /// <summary>
    /// How much nutrients are absorbed per digestion cycle
    /// </summary>
    [DataField("nutrientAbsorptionRate")]
    public float NutrientAbsorptionRate = 0.1f;

    /// <summary>
    /// How much waste is produced per digestion cycle
    /// </summary>
    [DataField("wasteProductionRate")]
    public float WasteProductionRate = 0.05f;

    /// <summary>
    /// Last time digestion was processed
    /// </summary>
    [ViewVariables]
    public TimeSpan LastDigestionTime = TimeSpan.Zero;
}
