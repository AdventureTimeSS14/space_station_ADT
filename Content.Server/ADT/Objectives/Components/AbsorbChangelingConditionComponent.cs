using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Требование генокраду украсть определённое количество штаммов ДНК
/// </summary>
[RegisterComponent, Access(typeof(AbsorbChangelingConditionSystem))]
public sealed partial class AbsorbChangelingConditionComponent : Component
{
    [DataField]
    public int AbsorbCount = 1;

    [DataField(required: true)]
    public LocId ObjectiveText;

    [DataField(required: true)]
    public LocId DescriptionText;

}
