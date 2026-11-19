using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Silicons.Borgs.Components;

[RegisterComponent]
public sealed partial class ADTBorgBurnRepairableComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    [DataField]
    public int CablePerUse = 1;

    [DataField]
    public SoundSpecifier? RepairSound;
}
