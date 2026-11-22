using Content.Shared.ADT.Chaplain.Prototypes;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Chaplain.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChaplainFollowerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Chaplain;

    [ViewVariables]
    public Dictionary<ProtoId<ChaplainPrayerPrototype>, TimeSpan> PrayerCooldowns = new();

    [DataField]
    public ProtoId<FactionIconPrototype> StatusIcon = "BelieverFaction";
}
