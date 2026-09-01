using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.SoftCrit.Components;

/// <summary>
///     Marker: the mob is currently downed by soft crit (so we only stand it back up ourselves).
/// </summary>
[RegisterComponent]
public sealed partial class SoftCritDownedComponent : Component;
