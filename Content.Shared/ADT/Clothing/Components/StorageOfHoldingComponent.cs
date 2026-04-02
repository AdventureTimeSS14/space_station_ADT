using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Clothing.Components;

/// <summary>
/// Компонент-заглушка. Нужен только лишь для "GerasSystem.cs"
/// Если в будущем для бездонных сумок добавят уникальный компонент, то этот можно спокойно удалить!!!
/// </summary>
[RegisterComponent]
public sealed partial class StorageOfHoldingComponent : Component
{
}
