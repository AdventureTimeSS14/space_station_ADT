using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Ursus;

[RegisterComponent, NetworkedComponent]
public sealed partial class UrsusStasisComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int DamageContained = 0;

    [DataField]
    public int DamageToWake = 20;

    [DataField]
    public List<string> StoredDamage = new()
    {
        "Blunt",
        "Piercing",
        "Slash",
        "Cold",
        "Heat",
        "Asphyxiation"
    };

    [DataField(required: true)]
    public DamageSpecifier Regen = new DamageSpecifier()
    {
        DamageDict = new()
        {
            { "Blunt", -1.5f },
            { "Piercing", -1f },
            { "Slash", -1f },
            { "Cold", -1 },
            { "Heat", -1 },
            { "Poison", -2f },
            { "Shock", -1f },
            { "Bloodloss", -1f }
        }
    };

    public TimeSpan NextUpdate = TimeSpan.Zero;
}
