using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Shared.ADT.Shields;

/// <summary>
///     Один тайл поля - тупой коллайдер. Всё состояние тайла живёт в
///     <see cref="ShieldTileData"/> на генераторе.
/// </summary>
[RegisterComponent]
public sealed partial class ShieldSegmentComponent : Component
{
    public EntityUid? Generator;

    /// <summary>Грид, к которому привязан сегмент.</summary>
    public EntityUid Grid;

    /// <summary>Позиция сегмента в тайлах грида. Ведёт в <c>Generator.Tiles[GridPos]</c>.</summary>
    public Vector2i GridPos;
}