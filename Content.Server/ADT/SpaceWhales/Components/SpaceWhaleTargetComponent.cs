using Content.Server.ADT.MobCaller;

namespace Content.Server.ADT.SpaceWhale;

/// <summary>
/// Marks an entity for a space whale target.
/// </summary>
[RegisterComponent]
public sealed partial class SpaceWhaleTargetComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public Entity<MobCallerComponent>? MobCaller;
}
