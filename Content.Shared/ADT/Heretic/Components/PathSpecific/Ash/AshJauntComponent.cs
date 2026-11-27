//

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Heretic.Components.PathSpecific.Ash;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class AshJauntComponent : Component
{
    [DataField]
    public EntProtoId OutEffect = "PolymorphAshJauntEndAnimation";

    [DataField]
    public TimeSpan EffectDuration = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan EndTime;

    [DataField]
    public bool SpawnedOutEffect;
}
