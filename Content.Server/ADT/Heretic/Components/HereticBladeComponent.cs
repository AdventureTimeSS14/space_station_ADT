using Content.Shared.ADT.Combat;

namespace Content.Server.Heretic.Components;

[RegisterComponent]
public sealed partial class HereticBladeComponent : Component
{
    [DataField] public string? Path;
    [DataField]
    public List<IComboEffect> EventsOnPickup = new List<IComboEffect> { };
}
