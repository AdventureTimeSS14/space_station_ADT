using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Fuel;

/// <summary>
/// Stores "fuel time" and allows refueling an entity by inserting burnable material items (stacks with PhysicalComposition).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FuelableComponent : Component
{
    /// <summary>
    /// How many seconds of burn-time 1 unit of "material volume" provides.
    /// Keys are material prototype IDs - only materials listed here can be used as fuel.
    /// </summary>
    [DataField("fuelTimePerMaterialUnit")]
    public Dictionary<string, float> FuelTimePerMaterialUnit = new()
    {
        { "Coal", 1.0f },
        { "Wood", 0.7f },
        { "Cloth", 0.5f },
        { "Cardboard", 0.5f },
        { "Cotton", 0.4f },
        { "Paper", 0.3f }
    };

    [DataField("fuelCapacitySeconds")]
    public float FuelCapacitySeconds = 600f;

    [DataField("fuelSeconds")]
    public float FuelSeconds = 0f;

    [DataField("burnRate")]
    public float BurnRate = 1.0f;

    [DataField("maxFireStacks")]
    public float MaxFireStacks = 10f;

    [DataField("insertDelay")]
    public float InsertDelay = 1.0f;

    [DataField("baseLightEnergy")]
    public float BaseLightEnergy = 0f;


    [DataField("baseLightRadius")]
    public float BaseLightRadius = 0f;
}

