using Content.Server.ADT.Objectives.Systems;

namespace Content.Server.ADT.Objectives.Components;

/// <summary>
/// An objective condition that requires the player to have the "Cascade" code at the station.
/// </summary>
[RegisterComponent, Access(typeof(CascadeConditionSystem))]
public sealed partial class CascadeConditionComponent : Component
{
    [DataField("needSupermatter")]
    public bool Supermatter = false;
}
