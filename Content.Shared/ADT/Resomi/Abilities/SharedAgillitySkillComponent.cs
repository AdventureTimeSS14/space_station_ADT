using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.Resomi.Abilities;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AgillitySkillComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool Active = false;

    /// <summary>
    /// Может ли игрок запрыгивать на столы. Если false, он будет только ускоряться.
    /// </summary>
    [DataField("jumpEnabled")]
    public bool JumpEnabled = true;

    [DataField("action", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? SwitchAgilityAction = "SwitchAgilityAction";

    [ViewVariables]
    public EntityUid? SwitchAgilityActionEntity;

    /// <summary>
    /// Сколько стамины будет отниматься при прыжке на стол
    /// </summary>
    [DataField("staminaDamageOnJump")]
    public float StaminaDamageOnJump = 6f;

    /// <summary>
    /// Пассивная трата стамины
    /// </summary>
    [DataField("staminaDamagePassive")]
    public float StaminaDamagePassive = 3f;

    [DataField("sprintSpeedModifier")]
    public float SprintSpeedModifier = 1.35f;

    /// <summary>
    /// Частота пассивной траты стамины
    /// </summary>
    [DataField("delay")]
    public double Delay = 1.0;

    public TimeSpan NextUpdate;
}
