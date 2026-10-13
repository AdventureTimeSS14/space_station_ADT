using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Colossus;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTColossusBoltComponent : Component
{
    [DataField]
    public bool GatherRock = true;

    [DataField]
    public bool DestroyWalls = true;

    [DataField]
    public DamageSpecifier ObjectDamage = new()
    {
        DamageDict =
        {
            { "Structural", 100 },
        },
    };

    [DataField]
    public float ObjectDamageMaxMultiplier = 2.5f;
}
