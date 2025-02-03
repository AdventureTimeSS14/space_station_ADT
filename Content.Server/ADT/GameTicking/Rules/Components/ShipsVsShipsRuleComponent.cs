using Robust.Shared.Player;

namespace Content.Server.ADT.GameTicking.Rules;

[RegisterComponent]
public sealed partial class ShipsVsShipsRuleComponent : Component
{
    // Тип победы в игре (по умолчанию - нейтральный)
    [DataField]
    public WinType WinType = WinType.Neutral;

    // Сторона, которая одержала победу (по умолчанию - неизвестная)
    [DataField]
    public Side WinSide = Side.Unknown;

    // Условия, при которых сторона может одержать победу
    [DataField]
    public HashSet<WindConditionInfo> WinConditions = new();

    // Словарь для хранения карт, связанных с каждой стороной
    [DataField]
    public Dictionary<Side, EntityUid> Maps = new();

    // Словарь для хранения шаттлов, связанных с каждой стороной
    [DataField]
    public Dictionary<Side, EntityUid> Shuttles = new();

    // Общее количество всех игроков в игре
    [DataField]
    public int TotalAllPlayers;

    // Словарь для хранения игроков, разделенных по сторонам
    [DataField]
    public Dictionary<Side, HashSet<ICommonSession>> Players = new();

    // Минимальный процент игроков, которые должны быть убиты для проигрыша стороны (по умолчанию 80%)
    [DataField]
    public float MinDiedPercent = 0.8f;

    // Флаг, указывающий, разрешено ли атаковать FTL (сверхсветовое перемещение)
    [DataField]
    public bool CanAttackFtl;

    // Время, необходимое для атаки FTL
    [DataField]
    public TimeSpan AttackFtlTime;

    // Задержка перед возможностью атаки FTL (по умолчанию 15 минут)
    [DataField]
    public TimeSpan AttackFtlDelay = TimeSpan.FromMinutes(15);

    // Сообщение, отправляемое при атаке FTL
    [DataField]
    public string AttackFtlMessage = "ships-vs-ships-attack-ftl-message";

    // Отправитель сообщения при атаке FTL
    [DataField]
    public string AttackFtlSender = "ships-vs-ships-attack-ftl-sender";

    // Набор сторон, которые могут вызвать экстренную эвакуацию
    [DataField]
    public HashSet<Side> CanCallEmergency = new()
    {
        Side.Defenders, // Только защитники могут вызвать экстренную эвакуацию
    };

    // Словарь для хранения противников каждой стороны
    [DataField]
    public Dictionary<Side, Side> EnemySides = new()
    {
        { Side.Attackers, Side.Defenders }, // Нападающие против защитников
        { Side.Defenders, Side.Attackers }, // Защитники против нападающих
    };
}

// Структура для хранения информации о условиях победы
public struct WindConditionInfo(Side side, WinCondition condition)
{
    public readonly Side Side = side; // Сторона, к которой относится условие победы
    public readonly WinCondition Condition = condition; // Условие победы
}

// Перечисление типов победы
public enum WinType : byte
{
    Major,   // Основная победа
    Minor,   // Второстепенная победа
    Neutral, // Нейтральный результат
}

// Перечисление условий победы
public enum WinCondition : byte
{
    MostDied,              // Победа по количеству убитых
    CallEmergency,         // Победа через вызов экстренной эвакуации
    Retreat,               // Победа через отступление
    NukeExploded,          // Победа через взрыв ядерного оружия
    NukeExplodedWrongPlace,// Победа через взрыв ядерного оружия в неправильном месте
}

// Перечисление сторон в игре
public enum Side : byte
{
    Unknown,   // Неизвестная сторона
    Attackers, // Нападающие
    Defenders, // Защитники
}
