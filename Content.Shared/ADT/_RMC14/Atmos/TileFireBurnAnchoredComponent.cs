using Content.Shared.Damage;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent]
public sealed partial class TileFireBurnAnchoredComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextBurnAt;
}
