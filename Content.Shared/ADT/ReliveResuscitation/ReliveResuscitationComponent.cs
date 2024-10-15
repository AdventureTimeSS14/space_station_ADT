using Robust.Shared.GameStates;

namespace Content.Shared.ADT.ReliveResuscitation;

[RegisterComponent, NetworkedComponent]
/// <summary>
/// Этот компонент вешается на сущность, при крит состоянии у неё, можно попробовать провести СЛР(Сердечно-лёгочную реанимацию),
/// и попытаться реанимировать убирая удушение взамен на добавление грубого урона.
/// by Шрёдька <3 (Schrodinger71)
/// </summary>
public sealed partial class ReliveResuscitationComponent : Component
{
    /// <summary>
    /// How long it takes to apply the damage.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delay")]
    public float Delay = 3f;
}

