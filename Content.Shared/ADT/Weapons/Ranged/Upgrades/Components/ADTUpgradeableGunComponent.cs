using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Weapons.Ranged.Upgrades.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTUpgradeableGunComponent : Component
{
    [DataField]
    public string UpgradesContainerId = "upgrades";

    [DataField]
    public EntityWhitelist Whitelist = new();

    [DataField, AutoNetworkedField]
    public int MaxModCapacity = 100;

    [DataField]
    public float RemoveDelay = 1f;

    [DataField]
    public ProtoId<ToolQualityPrototype> RemoveTool = "Prying";

    [DataField]
    public Dictionary<string, SpriteSpecifier.Rsi> ChassisSprites = new();

    [DataField]
    public SpriteSpecifier.Rsi? StockSprite;

    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Items/screwdriver2.ogg");

    [DataField]
    public SoundSpecifier? RemoveSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");
}
