using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Bubblegum.Abilities;

/// <summary>
/// «Кровавый прыжок» — телепорт к луже крови у цели, активирует Enrage.
/// SS13: проверяет лужу под собой и в кольце радиусом 4..5 от цели.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BubblegumBloodWarpComponent : Component
{
    /// <summary>
    /// Внешний радиус (включительно) поиска пуддла около цели.
    /// </summary>
    [DataField]
    public float OuterRange = 5f;

    /// <summary>
    /// Внутренний радиус (исключаемый).
    /// </summary>
    [DataField]
    public float InnerRange = 4f;

    /// <summary>
    /// Радиус, в котором ищем лужу под боссом (нужна, чтобы «нырнуть»).
    /// </summary>
    [DataField]
    public float SelfRange = 1f;

    /// <summary>
    /// Дистанция, при которой Blood Warp отказывается работать (boss уже в упор).
    /// </summary>
    [DataField]
    public float AdjacentRange = 1.5f;

    /// <summary>
    /// Время задержки на «нырок» (decoy остаётся до перемещения).
    /// </summary>
    [DataField]
    public float SinkTime = 0.5f;

    /// <summary>
    /// Звук «нырка» в кровь.
    /// </summary>
    [DataField]
    public SoundSpecifier EnterSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    /// <summary>
    /// Звук «выныривания».
    /// </summary>
    [DataField]
    public SoundSpecifier ExitSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");
}
