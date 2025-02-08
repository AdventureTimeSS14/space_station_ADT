using Robust.Shared.GameStates;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.OnGhostAttemtpDamage;

[RegisterComponent]
public sealed partial class OnGhostAttemtpDamageComponent : Component
{
    [DataField]
    public ProtoId<DamageTypePrototype> BloodlossDamageType = "Bloodloss";
}

/*
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝

*/
