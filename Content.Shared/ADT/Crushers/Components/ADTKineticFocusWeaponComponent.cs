using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent]
public sealed partial class ADTKineticFocusWeaponComponent : Component
{
    [DataField(required: true)]
    public ADTKineticDiscipline Discipline;

    [DataField]
    public bool TrackMelee = true;

    [DataField]
    public bool TrackRanged = true;

    [DataField]
    public TimeSpan Window = TimeSpan.FromSeconds(5);

    [DataField]
    public EntProtoId Effect = "ADTStatusEffectKineticInterference";

    [DataField]
    public TimeSpan EffectDuration = TimeSpan.FromSeconds(8);
}

public enum ADTKineticDiscipline : byte
{
    Accelerator,
    Crusher,
}
