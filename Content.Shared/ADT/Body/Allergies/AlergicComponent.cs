using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Body.Allergies;

[RegisterComponent, NetworkedComponent]
public sealed partial class AllergicComponent : Component
{
    [DataField]
    public List<ProtoId<ReagentPrototype>> Triggers = new();

    [DataField]
    public bool InShock = false;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public TimeSpan NextShockEvent = TimeSpan.Zero;

    [DataField]
    public float AllergyStack = 0f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float MaximumStack = 30f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float StackFade = -0.2f;

    [ViewVariables(VVAccess.ReadOnly)]
    [DataField]
    public TimeSpan NextStackFade = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public TimeSpan StackFadeRate = TimeSpan.FromSeconds(5);

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
