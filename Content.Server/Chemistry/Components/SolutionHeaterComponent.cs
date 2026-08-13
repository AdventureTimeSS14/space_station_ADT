namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed partial class SolutionHeaterComponent : Component
{
    /// <summary>
    /// How much heat is added per second to the solution, taking upgrades into account.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HeatPerSecond;

    // ADT-Tweak-Start
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float HeatMultiplier = 1f;
    // ADT-Tweak-End
}
