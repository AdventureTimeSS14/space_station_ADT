using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.Allergies;

[RegisterComponent, NetworkedComponent]
public sealed partial class AllergicComponent : Component
{
    [DataField]
    public List<ProtoId<ReagentPrototype>> Triggers = new();

    /// <summary>
    /// Время следующего события анафилактического шока.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public TimeSpan NextShockEvent = TimeSpan.Zero;

    /// <summary>
    /// Минимальное время в секундах до следующего события анафилактического шока.
    /// </summary>
    [DataField]
    public float MinimumTimeTilNextShockEvent = 5f;

    /// <summary>
    /// Максимальное время в секундах до следующего события анафилактического шока.
    /// </summary>
    [DataField]
    public float MaximumTimeTilNextShockEvent = 10f;

    /// <summary>
    /// Стак аллергии.
    /// </summary>
    [DataField]
    public float AllergyStack = 0f;

    /// <summary>
    /// Максимальный размер стака аллергии.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MaximumStack = 30f;

    /// <summary>
    /// Значение затухания (декремента) стака.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float StackFade = -0.2f;

    /// <summary>
    /// Скорость затухания (декремента) стака.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public TimeSpan StackFadeRate = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Время следующего декремента стака.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public TimeSpan NextStackFade = TimeSpan.Zero;

    /// <summary>
    /// Значение роста стака при метаболизме аллергена.
    /// </summary>

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float StackGrow = 0.5f;

    /// <summary>
    /// Минимальное количество назначенных аллергенов при инициализации компонента.
    /// </summary>
    [DataField(readOnly: true)]
    public int Min = 1;

    /// <summary>
    /// Максимальное количество назначенных аллергенов при инициализации компонента.
    /// </summary>
    [DataField(readOnly: true)]
    public int Max = 3;
}
