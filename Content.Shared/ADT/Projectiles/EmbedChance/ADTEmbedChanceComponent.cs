using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Projectiles.EmbedChance;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTEmbedChanceComponent : Component
{
    [DataField]
    public float Chance = 0.25f;

    [DataField]
    public EntityWhitelist? Whitelist;
}
