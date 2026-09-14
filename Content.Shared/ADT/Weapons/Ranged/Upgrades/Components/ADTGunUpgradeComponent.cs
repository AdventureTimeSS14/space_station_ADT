using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTGunUpgradeComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Cost = 30;

    [DataField]
    public List<ProtoId<TagPrototype>> Tags = new();

    [DataField]
    public int MaximumOfType;

    [DataField]
    public LocId ExamineText;

    [DataField]
    public float VolleyFalloff = 0.2f;
}
