using Robust.Shared.GameStates;

namespace Content.Shared.ADT.ReliveResuscitation;

/// <summary>
/// Компонент, позволяющий проводить сердечно-лёгочную реанимацию сущности в критическом состоянии.
/// Убирает удушение взамен на добавление грубого урона.
/// by Шрёдька <3 (Schrodinger71)
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ReliveResuscitationComponent : Component
{
    /// <summary>
    /// Время проведения реанимации в секундах
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("delay")]
    public float Delay = 3.3f;
}

