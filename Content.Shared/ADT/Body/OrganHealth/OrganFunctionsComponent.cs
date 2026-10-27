using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Body.OrganHealth;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class OrganFunctionsComponent : Component
{
    [ViewVariables]
    public HashSet<ProtoId<OrganCategoryPrototype>> ExpectedCategories = new();

    [ViewVariables, AutoNetworkedField]
    public bool Blind;

    [ViewVariables, AutoNetworkedField]
    public float Blur;

    [ViewVariables, AutoNetworkedField]
    public bool Mute;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? HeartAttackStart;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan HeartAttackSuppressedUntil;

    [ViewVariables]
    public List<TimeSpan> DefibrillatorZaps = new();
}
