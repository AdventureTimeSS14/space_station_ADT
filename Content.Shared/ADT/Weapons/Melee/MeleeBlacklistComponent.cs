using System.Numerics;
using Content.Shared.Physics;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Melee.MeleeBlacklist;

[RegisterComponent]
public sealed partial class MeleeBlacklistComponent : Component
{
    [DataField]
    public EntityWhitelist? Whitelist;
    [DataField]
    public EntityWhitelist? Blacklist;
}
