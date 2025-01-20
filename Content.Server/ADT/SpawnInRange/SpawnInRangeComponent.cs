namespace Content.Server.ADT.Generation;

[RegisterComponent]
public sealed partial class SpawnInRangeComponent : Component
{
    /// <summary>
    /// прототипы, которые заспивнит в радиусе
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> ProtosToSpawn = new();

    /// <summary>
    /// все min и max отвечают за минимальные и максимальные координаты для спавна. Прототипы не могут быть созданны на координатах от -40 и до 40.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinX;

    [DataField, AutoNetworkedField]
    public float MaxX;

    [DataField, AutoNetworkedField]
    public float MinY;

    [DataField, AutoNetworkedField]
    public float MaxY;

    /// <summary>
    /// вокруг какого радиуса вокруг прототипа не могут заспавниться другие прототипы
    /// </summary>

    [DataField, AutoNetworkedField]
    public float ClearRadiusAroundSpawned = 30;
}
