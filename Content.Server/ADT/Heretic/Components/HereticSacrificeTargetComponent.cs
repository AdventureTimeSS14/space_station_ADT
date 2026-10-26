using Content.Server.Heretic.EntitySystems;

namespace Content.Server.Heretic.Components;

[RegisterComponent]
[Access(typeof(HereticSystem))]
public sealed partial class HereticSacrificeTargetComponent : Component
{
    [ViewVariables]
    public readonly HashSet<EntityUid> Heretics = new();
}
