using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Chaplain.Components;

/// <summary>
/// Компонент, предоставляющий иммунитет к магии.
/// Существа с этим компонентом не могут быть выбраны в качестве цели магических способностей
/// и невосприимчивы к эффектам магии (еретик, ревенант и т.д.).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MagicImmunityComponent : Component
{
}
