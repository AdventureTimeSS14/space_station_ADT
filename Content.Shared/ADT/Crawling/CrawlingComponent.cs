using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Crawling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedCrawlingSystem))]
public sealed partial class CrawlingComponent : Component
{
    [ViewVariables, DataField("sprintSpeedModifier"), AutoNetworkedField]
    public float SprintSpeedModifier = 0.3f;

    [ViewVariables, DataField("walkSpeedModifier"), AutoNetworkedField]
    public float WalkSpeedModifier = 0.3f;
}
