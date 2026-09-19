using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Vehicle.Trailer;

/// <summary>
/// Маркер прицепа: каталка или мешок для трупов, цепляемый к сцепке транспорта.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ADTTrailerComponent : Component
{
}