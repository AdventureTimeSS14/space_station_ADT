using Content.Server.ADT._Ganimed.Objectives.Systems;
using Content.Server.Objectives.Components;

namespace Content.Server.ADT._Ganimed.Objectives.Components;

/// <summary>
/// Requires that a target dies once and only once.
/// Depends on <see cref="TargetObjectiveComponent"/> to function.
/// </summary>
[RegisterComponent, Access(typeof(TeachLessonConditionSystem))]
public sealed partial class TeachLessonConditionComponent : Component;