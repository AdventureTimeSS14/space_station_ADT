using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Shadowling;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTLesserShadowlingComponent : Component
{
    [DataField]
    public List<EntProtoId> Actions = new()
    {
        "ADTActionShadowlingGlare",
        "ADTActionShadowlingShadowWalk",
        "ADTActionShadowlingDarksight",
    };

    [DataField]
    public List<EntityUid> GrantedActions = new();

    [DataField]
    public Color EyeColor = Color.Red;

    [DataField, AutoNetworkedField]
    public TimeSpan SurgeryBacklashStun = TimeSpan.FromSeconds(12);

    [DataField, AutoNetworkedField]
    public float SurgeryBacklashDamage = 20f;
}
