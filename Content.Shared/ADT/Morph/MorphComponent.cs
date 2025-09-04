using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.Containers;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Morph;

[RegisterComponent, AutoGenerateComponentState, NetworkedComponent] //, AutoGenerateComponentState]
public sealed partial class MorphComponent : Component
{
    /// <summary>
    ///     Container for various consumable items
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Container Container = default!;
    public string ContainerId = "morphContainer";

    /// <summary>
    ///     Контейнер для костылей, а вернее подгрузки предметов для ГУИ мимикрии
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Container MimicryContainer = default!;
    public string MimicryContainerId = "mimicryContainer";

    /// <summary>
    ///     урон при касании
    /// </summary>
    [DataField]
    public DamageSpecifier DamageOnTouch = default!;
    /// <summary>
    ///     нужно исключительно для размножения
    /// </summary>
    [DataField]
    public string MorphSpawnProto = "ADTMorphGhostRole";

    /// <summary>
    ///     шанс скушать оружие ударом морфа
    /// </summary>
    [DataField]
    public float EatWeaponChanceOnHit = 0.2f;
    /// <summary>
    ///     шанс скушать оружие ударом по морфу
    /// </summary>
    [DataField]
    public float EatWeaponChanceOnHited = 0.5f;
    /// <summary>
    ///     количество еды, нужное чтобы скушать оруже
    /// </summary>
    [DataField]
    public int EatWeaponHungerReq = 5;

    /// <summary>
    /// после скольки морфов на станции ЦК объявит о них
    /// </summary>
    [DataField]
    public int DetectableCount = 6;

    /// <summary>
    ///     количество еды, нужное для открытия вентиляции
    /// </summary>
    [DataField]
    public int OpenVentFoodReq = 5;

    /// <summary>
    ///     количество еды, нужное для размножения
    /// </summary>
    [DataField]
    public int ReplicationFoodReq = 200;
    /// <summary>
    /// Звук обеда
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundDevour")]
    public SoundSpecifier? SoundDevour = new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };
    /// <summary>
    /// объява цк после массового размножения
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("soundReplication")]
    public SoundSpecifier? SoundReplication = new SoundPathSpecifier("/Audio/ADT/Announcements/announce_dig.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f),
    };
    /// <summary>
    /// время нужное для обеда
    /// </summary>
    [DataField("devourTime")]
    public float DevourTime = 3f;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public List<EntityUid> ContainedCreatures = new();
    /// <summary>
    /// вайтлист на обед
    /// </summary>
    [AutoNetworkedField]
    public List<EntityUid> MemoryObjects = new();
    [DataField]
    public EntityWhitelist? DevourWhitelist = new();

    //дальше идёт хлам, который вам не надо использовать
    [DataField("devourAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? DevourAction = "ActionMorphDevour";
    public EntityUid? DevourActionEntity;
    [DataField("memoryAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? MemoryAction = "ActionMorphRemember";
    public EntityUid? MemoryActionEntity;
    [DataField("replicationAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? ReplicationAction = "ActionMorphReplication";
    public EntityUid? ReplicationActionEntity;
    [DataField("mimicryAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? MimicryAction = "ActionMorphMimicry";
    public EntityUid? MimicryActionEntity;
    [DataField("ambushAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? AmbushAction = "ActionMorphAmbush";
    public EntityUid? AmbushActionEntity;
    [DataField("openVentAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? VentOpenAction = "ActionMorphVentOpen";
    public EntityUid? VentOpenActionEntity;
    // public List<HumanoidAppearanceComponent> ApperanceList = new();
    //нужен для работы мимикрии под гуманойдов, больше ничего
    //бла-бла-бла, это надо если хотите делать морф под гуманоидов не костылями
    // public (EntityUid, HumanoidAppearanceComponent) NullspacedHumanoid = default;

}

/// <summary>
/// копирование данных предметов для мимикрии. Работает ТОЛЬКО на простых предметах, гуманоидов или существ с телом отдельный метод
/// </summary>
public sealed partial class MorphDevourActionEvent : EntityTargetActionEvent
{
}
public sealed partial class MorphMimicryRememberActionEvent : EntityTargetActionEvent
{
}
public sealed partial class MorphReproduceActionEvent : InstantActionEvent
{
}
public sealed partial class MorphOpenRadialMenuEvent : InstantActionEvent
{
}
public sealed partial class MorphDevourActionEvent : EntityTargetActionEvent
{
}
public sealed partial class MorphAmbushActionEvent : InstantActionEvent
{
}
public sealed partial class MorphVentOpenActionEvent : EntityTargetActionEvent
{
}
[Serializable, NetSerializable] public sealed partial class EventMimicryActivate : BoundUserInterfaceMessage
{
    public NetEntity? Target { get; set; }
}
[Serializable, NetSerializable] public enum MimicryKey : byte
{
    Key
}
