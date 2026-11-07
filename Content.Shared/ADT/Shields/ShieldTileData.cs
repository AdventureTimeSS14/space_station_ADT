using Robust.Shared.Maths;

namespace Content.Shared.ADT.Shields;

/// <summary>Состояние тайла поля. Живёт на генераторе одним объектом.</summary>
public sealed class ShieldTileData
{
    /// <summary>Секунд до восстановления тайла после отключения.</summary>
    public float DisabledFor;

    /// <summary>Секунд до восстановления тайла после диффузии.</summary>
    public float DiffusedFor;

    /// <summary>Слабый щит на уровне пола (не блокирует движение).</summary>
    public bool Floor;

    /// <summary>Герметизирует ли тайл соседние тайлы корпуса. Грязный флаг: реестр переписывается только при изменении.</summary>
    public bool SealsActive;
}