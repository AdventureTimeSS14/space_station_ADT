using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Mining.Drill;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTDrillComponent : Component
{
    [DataField]
    public bool Active;

    [DataField]
    public bool Error;

    /// <summary>
    /// Вместимость внутреннего хранилища
    /// </summary>
    [DataField]
    public int OreCapacity = 1000;

    [DataField]
    public ProtoId<WeightedRandomOrePrototype> OreDistributionId = "RandomOreDistributionStandard";

    /// <summary>
    /// Минимальное количество предметов руды из одной залежи
    /// </summary>
    [DataField]
    public int MinOrePerDeposit = 1;

    /// <summary>
    /// Максимальное количество предметов руды из одной залежи
    /// </summary>
    [DataField]
    public int MaxOrePerDeposit = 4;

    /// <summary>
    /// Необходимое количество опор для работы
    /// </summary>
    [DataField]
    public int BraceRequired = 2;

    [DataField]
    public TimeSpan TickInterval = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Шанс поломки бура
    /// </summary>
    [DataField]
    public float BreakChance = 0.02f;

    [DataField]
    public string ContainerId = "drill-ore-storage";

    [DataField]
    public float UnloadRange = 1.5f;

    [DataField]
    public List<EntProtoId> OreContainers = new();

    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/Items/drill_use.ogg");

    [DataField]
    public SoundSpecifier StopSound = new SoundPathSpecifier("/Audio/Items/change_drill.ogg");

    [DataField]
    public SoundSpecifier ErrorSound = new SoundPathSpecifier("/Audio/Machines/buzz-sigh.ogg");

    [ViewVariables]
    public TimeSpan NextTick;
}