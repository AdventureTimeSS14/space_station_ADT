using Content.Shared.ADT.Salvage.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Salvage.Components;

/// <summary>
/// Моб, созданный тендрилом. При смерти удаляется из списка его спавнов
/// </summary>
[RegisterComponent]
public sealed partial class TendrilMobComponent : Component
{
    public EntityUid? Tendril;
}
