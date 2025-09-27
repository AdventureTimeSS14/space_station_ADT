using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Mining.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class GibtoniteComponent : Component
{
    #region База
    /// <summary>
    /// Активный ли гибтонит.
    /// </summary>
    public bool Active = false;

    /// <summary>
    /// Состояние спрайта руды при Extracted.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("state")]
    public GibtoniteState State;
    #endregion

    #region Время
    /// <summary>
    /// Запись гибтонита в активном состоянии.
    /// </summary>
    [DataField]
    public TimeSpan ReactionTime;

    /// <summary>
    /// Макс. время в активном состоянии.
    /// </summary>
    [DataField]
    public float ReactionMaxTime = 10f;

    /// <summary>
    /// Сколько времени прошло с активации.
    /// </summary>
    [DataField("elapsedTime")]
    public float ReactionElapsedTime;
    #endregion

    #region Всё для прототипов
    /// <summary>
    /// Небольшая переменная, которая будет отвечать, это гибтонит ище в камне или выпавшая руда.
    /// </summary>
    [DataField("extracted")]
    public bool Extracted;

    /// <summary>
    /// Ссылка на прототип для спауна руды.
    /// </summary>
    [DataField]
    public EntProtoId OrePrototype = "ADTGibtonite";

    /// <summary>
    /// Значения для взрыва. Думаю по названиям переменных всё понятно... так ведь?
    /// </summary>
    [DataField("maxIntensity")]
    public float MaxIntensity = 450f;

    [DataField("minIntensity")]
    public float MinIntensity = 225f;
    #endregion
}

public enum GibtoniteState
{
    OhFuck,
    Normal,
    Nothing
}

[Serializable, NetSerializable]
public enum GibtonitState : byte
{
    State,
}