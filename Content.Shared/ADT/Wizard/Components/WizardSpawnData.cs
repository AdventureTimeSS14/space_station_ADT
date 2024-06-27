namespace Content.Shared.Wizard;

[ImplicitDataDefinitionForInheritors]
public abstract partial class WizardSpawnData
{

}

/// <summary>
/// Spawns 1 at the caster's feet.
/// </summary>
public sealed partial class TargetCasterPos : WizardSpawnData { }

/// <summary>
/// Targets the 3 tiles in front of the caster.
/// </summary>
public sealed partial class TargetInFront : WizardSpawnData
{
    [DataField("width")] public int Width = 3;
}
