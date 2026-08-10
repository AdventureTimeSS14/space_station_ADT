using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Thunderdome;

[Serializable, NetSerializable]
public sealed partial class ThunderdomeLoadoutEuiState : EuiStateBase
{
    public List<ThunderdomeLoadoutOption> Weapons { get; }
    public List<ThunderdomeLoadoutOption> Equipment { get; }
    public int PlayerCount { get; }

    public ThunderdomeLoadoutEuiState(List<ThunderdomeLoadoutOption> weapons, List<ThunderdomeLoadoutOption> equipment, int playerCount)
    {
        Weapons = weapons;
        Equipment = equipment;
        PlayerCount = playerCount;
    }
}

[Serializable, NetSerializable]
public sealed partial class ThunderdomeLoadoutOption
{
    public int Index { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SpritePrototype { get; set; } = string.Empty;
}

[Serializable, NetSerializable]
public sealed partial class ThunderdomeLoadoutSelectedMessage : EuiMessageBase
{
    public int WeaponIndex { get; }
    public int EquipmentIndex { get; }

    public ThunderdomeLoadoutSelectedMessage(int weaponIndex, int equipmentIndex)
    {
        WeaponIndex = weaponIndex;
        EquipmentIndex = equipmentIndex;
    }
}
