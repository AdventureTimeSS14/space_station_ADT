using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;
using Content.Shared.Tag;

namespace Content.Shared.Weapons.Melee.Upgrades.Components;

/// <summary>
/// Базовый компонент для улучшений оружия ближнего боя
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MeleeUpgradeSystem))]
public sealed partial class MeleeUpgradeComponent : Component
{
    [DataField]
    public LocId ExamineText;

    [DataField("tags")]
    public HashSet<ProtoId<TagPrototype>> Tags = new();
}
