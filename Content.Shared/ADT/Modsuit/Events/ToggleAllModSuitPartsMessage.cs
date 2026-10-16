using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ModSuits;

/// <summary>
/// Развернуть или сложить МОД целиком. Направление выбирается по текущему состоянию.
/// </summary>
[Serializable, NetSerializable]
public sealed class ToggleAllModSuitPartsMessage : BoundUserInterfaceMessage
{
}
