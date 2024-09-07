namespace Content.Shared.ComponentalActions;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ComponentalActionsSpawnData
{

}

/// <summary>
/// Spawns 1 at the caster's feet.
/// </summary>
public sealed partial class TargetCasterPos : ComponentalActionsSpawnData { }

/// <summary>
/// Targets the 3 tiles in front of the caster.
/// </summary>
public sealed partial class TargetInFront : ComponentalActionsSpawnData
{
    [DataField("width")] public int Width = 3;
}
