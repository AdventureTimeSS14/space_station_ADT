using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Implants.ImplantBoost;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class ImplantBoostComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MovementSpeedModifier = 1f;

    [DataField, AutoNetworkedField]
    public float AttackSpeedModifier = 1f;

    [DataField, AutoNetworkedField]
    public float DoAfterSpeedModifier = 1f;

    [DataField, AutoNetworkedField]
    public TimeSpan? EndTime;

    public ImplantBoostSettings? Settings;

    public int ColorAccumulator;
}
