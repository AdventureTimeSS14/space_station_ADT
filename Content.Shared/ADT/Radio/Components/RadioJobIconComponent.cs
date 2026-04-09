using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Radio.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class RadioJobIconComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public ProtoId<JobIconPrototype> JobIconId = "JobIconNoId";

    [DataField]
    [AutoNetworkedField]
    public string JobName = string.Empty;
}
