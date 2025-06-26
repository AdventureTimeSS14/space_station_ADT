using Robust.Shared.GameStates;

namespace Content.Shared._Ganimed.SeedDna.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class DnaDiskComponent : Component
{
    public SeedDataDto? SeedData;
}
