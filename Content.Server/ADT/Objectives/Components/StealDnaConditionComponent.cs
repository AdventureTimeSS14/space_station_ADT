using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Требование генокраду украсть определённое количество штаммов ДНК
/// </summary>
[RegisterComponent, Access(typeof(StealDnaConditionSystem))]
public sealed partial class StealDnaConditionComponent : Component
{
    public int AbsorbDnaCount;

    public int MaxDnaCount = 16;
    public int MinDnaCount = 8;


    [DataField(required: true)]
    public LocId ObjectiveText;

    [DataField(required: true)]
    public LocId DescriptionText;

}
