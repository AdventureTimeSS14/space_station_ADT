using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Sets the target for <see cref="TargetObjectiveComponent"/> to a random person.
/// </summary>
[RegisterComponent, Access(typeof(KillPhantomImmunePersonConditionSystem))]
public sealed partial class PickPhantomImmunePersonComponent : Component
{
}
