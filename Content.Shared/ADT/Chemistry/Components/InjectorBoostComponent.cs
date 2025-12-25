// ADT-Tweak (P4A) Ускорение Шприцов на койках и каталках
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Chemistry.Components;

/// <summary>
/// Метка на мебель. Если цель пристегнута к этой мебели,
/// инъекция ускоряется (множитель берётся из InjectorComponent).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InjectorBoostComponent : Component
{
}
