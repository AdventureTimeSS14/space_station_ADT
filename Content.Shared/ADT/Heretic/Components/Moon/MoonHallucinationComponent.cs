using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Moon;

[RegisterComponent, NetworkedComponent]
public sealed partial class MoonHallucinationComponent : Component
{
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(15);

    public TimeSpan DespawnAt = TimeSpan.Zero;

    public NetEntity? Victim;
}
