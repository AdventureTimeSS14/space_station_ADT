using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Crushers.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ADTKineticFocusComponent : Component
{
    [DataField]
    public ADTKineticDiscipline Discipline;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan LastHit;
}
