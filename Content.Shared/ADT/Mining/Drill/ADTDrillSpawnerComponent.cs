using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Mining.Drill;

/// <summary>
/// Конфигурация спавна существ/предметов вокруг работающего бура.
/// Вынесена отдельно, чтобы в будущем можно было настраивать любые таблицы спавна, а не только рудных крабов.
/// </summary>
[RegisterComponent]
public sealed partial class ADTDrillSpawnerComponent : Component
{
    /// <summary>
    /// таблица для спавна
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomEntityPrototype> SpawnTable = "ADTDrillOreSpawns";

    /// <summary>
    /// Шанс спавна
    /// </summary>
    [DataField]
    public float SpawnChance = 0.1f;

    /// <summary>
    /// Радиус случайной точки появления вокруг бура
    /// </summary>
    [DataField]
    public float SpawnRadius = 5f;
}