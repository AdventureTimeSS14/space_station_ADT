using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Power.PTL;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerTransmissionLaserComponent : Component
{
    [DataField, AutoNetworkedField] public bool Active = false;

    [DataField, AutoNetworkedField] public double SpesosHeld = 0f;
    [DataField, AutoNetworkedField] public double MinSpesosEject = 10000f;
    [DataField] public double MinShootPower = 1e6; // 1 MJ
    [DataField] public double MaxEnergyPerShot = 1e8; // 100 MJ, Used to limit evil effects, but not coded so doesn't do anything

    [DataField, AutoNetworkedField] public float ShootDelay = 5f;
    [DataField, AutoNetworkedField] public float ShootDelayIncrement = 3f;
    [DataField, AutoNetworkedField] public MinMax ShootDelayThreshold = new MinMax(5, 60);
    [DataField, AutoNetworkedField] public bool ReversedFiring = false;
    [ViewVariables(VVAccess.ReadOnly)] public TimeSpan NextShotAt = TimeSpan.Zero;
    [ViewVariables(VVAccess.ReadOnly)] public TimeSpan RadDecayTimer = TimeSpan.Zero;

    [DataField] public DamageSpecifier BaseBeamDamage;

    /// <summary>
    ///     The factor that power (in MJ) is multiplied by to calculate radiation and blinding.
    /// </summary>
    [DataField] public double EvilMultiplier = 1;
}
