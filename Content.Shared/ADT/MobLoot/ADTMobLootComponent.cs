using Robust.Shared.GameStates;
using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.MobLoot;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTMobLootComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, float> Loots = new();
}
