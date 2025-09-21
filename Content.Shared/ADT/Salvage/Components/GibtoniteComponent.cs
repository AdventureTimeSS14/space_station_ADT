using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Salvage.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GibtoniteComponent : Component
{
    /// <summary>
    /// Активный ли гибтонит.
    /// </summary>
    public bool Active = false;

    /// <summary>
    /// Небольшая переменная, которая будет отвечать, это гибтонит ище в камне или выпавшая руда.
    /// </summary>
    [DataField("extracted")]
    public bool Extracted;

    /// <summary>
    /// Запись гибтонита в активном состоянии.
    /// </summary>
    [DataField]
    public TimeSpan ReactionTime;

    /// <summary>
    /// Макс. время в активном состоянии.
    /// </summary>
    [DataField]
    public float MaxReactionTime = 10f;

    /// <summary>
    /// Костыль ебанный.
    /// </summary>
    [DataField]
    public float Power = 1f;
}