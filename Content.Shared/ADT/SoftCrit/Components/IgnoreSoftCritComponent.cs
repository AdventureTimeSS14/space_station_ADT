using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.SoftCrit.Components;

/// <summary>
///     Entities with this component never enter soft crit (e.g. silicons, NPCs).
/// </summary>
[RegisterComponent]
public sealed partial class IgnoreSoftCritComponent : Component;
