using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player is on the emergency shuttle's grid when docking to CentCom.
/// </summary>
[RegisterComponent, Access(typeof(AbsorbDnaConditionSystem))]
public sealed partial class AbsorbDnaConditionComponent : Component
{
    [DataField]
    public int AbsorbDnaCount = 4;

    [DataField]
    public int MaxDnaCount = 4;

    [DataField]
    public int MinDnaCount = 2;

    [DataField(required: true)]
    public LocId ObjectiveText;
    [DataField(required: true)]
    public LocId DescriptionText;

}
