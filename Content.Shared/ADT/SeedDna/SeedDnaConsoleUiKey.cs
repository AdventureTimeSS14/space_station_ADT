using Robust.Shared.Serialization;

namespace Content.Shared.ADT.SeedDna;

/// <summary>
/// Маркер, необходимый для компонента "ActivatableUI"
/// </summary>
[Serializable, NetSerializable]
public enum SeedDnaConsoleUiKey : byte
{
    Key,
}
