using System.Numerics;

namespace Content.Shared.ADT.Clothing;

[RegisterComponent]
public sealed partial class ADTOversizedSpriteComponent : Component
{
    /// <summary>
    /// Сколько пикселей должна занимать одна клетка после ужатия.
    /// </summary>
    [DataField]
    public int CellSize = 32;

    /// <summary>
    /// Размер ячейки исходного RSI. Если null, берётся из meta.json самого RSI.
    /// </summary>
    [DataField]
    public int? SourceSize;

    /// <summary>
    /// Полностью ручной множитель масштаба. Если задан, <see cref="CellSize"/> и <see cref="SourceSize"/>
    /// не используются.
    /// </summary>
    [DataField]
    public Vector2? Scale;

    /// <summary>
    /// Ужимать ли спрайт самой сущности
    /// </summary>
    [DataField]
    public bool ScaleWorldSprite = true;

    /// <summary>
    /// Ужимать ли слои, которые вещь добавляет на спрайт носителя, когда её надели.
    /// </summary>
    [DataField]
    public bool ScaleEquipped = true;

    /// <summary>
    /// Ужимать ли слои, которые вещь добавляет на спрайт носителя, когда её держат в руке.
    /// </summary>
    [DataField]
    public bool ScaleInHand = true;

    /// <summary>
    /// Оставлять ли карты смещения на ужатых слоях. По умолчанию они снимаются, потому что карта смещения расы
    /// нарисована под ячейку 32x32 и на большом спрайте растянулась бы на всю ячейку, обрезая вещь.
    /// </summary>
    [DataField]
    public bool UseDisplacement;

    /// <summary>
    /// Дополнительный сдвиг надетых слоёв в клетках.
    /// </summary>
    [DataField]
    public Vector2 EquippedOffset = Vector2.Zero;

    /// <summary>
    /// Дополнительный сдвиг слоёв предмета в руках, в клетках.
    /// </summary>
    [DataField]
    public Vector2 InHandOffset = Vector2.Zero;
}
