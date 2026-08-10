using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Thunderdome;

[Serializable, NetSerializable]
public sealed partial class ThunderdomeLoadoutEuiState : EuiStateBase
{
    public List<ThunderdomeLoadoutOption> Weapons { get; }
    public int PlayerCount { get; }

    public ThunderdomeLoadoutEuiState(List<ThunderdomeLoadoutOption> weapons, int playerCount)
    {
        Weapons = weapons;
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

    public ThunderdomeLoadoutSelectedMessage(int weaponIndex)
    {
        WeaponIndex = weaponIndex;
    }
}
