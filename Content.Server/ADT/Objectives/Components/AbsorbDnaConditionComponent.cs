using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Требование генокраду украсть определённое количество штаммов ДНК
/// </summary>
[RegisterComponent, Access(typeof(StealDnaConditionSystem))]
public sealed partial class StealDnaConditionComponent : Component
{
    [DataField]
    public int AbsorbDnaCount = 4;

    [DataField]
    public int MaxDnaCount = 13;

    [DataField]
    public int MinDnaCount = 10;

    [DataField(required: true)]
    public LocId ObjectiveText;
    [DataField(required: true)]
    public LocId DescriptionText;

}
