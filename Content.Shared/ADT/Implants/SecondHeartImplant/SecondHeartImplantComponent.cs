using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.Implants.SecondHeartImplant;

[RegisterComponent]
public sealed partial class SecondHeartImplantComponent : Component
{
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/ADT/Effects/ImplantsEffects/second-heart-activate.ogg");

    [DataField]
    public DamageSpecifier ResidualDamage = new()
    {
        DamageDict = new()
        {
            ["Asphyxiation"] = 50
        }
    };
}
