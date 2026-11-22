using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Particles.Visuals;

/// <summary>
/// Gives an entity a chance to spark whenever damaged (ignition and visuals).
/// </summary>
[RegisterComponent]
public sealed partial class SparksOnDamageComponent : Component
{
    /// <summary>Particle effect spawned as the spark burst.</summary>
    [DataField]
    public ProtoId<ParticleEffectPrototype> Effect = "ADTMachineHitSparks";

    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("sparks");

    /// <summary>Chance to emit sparks for each damage event, from 0-1 (0%-100%).</summary>
    [DataField]
    public float Chance = 0.35f;

    // Light
    [DataField]
    public Color LightColor = Color.FromHex("#FFD16A");

    [DataField]
    public float LightRadius = 1.5f;

    [DataField]
    public float LightEnergy = 4f;

    [DataField]
    public float LightDuration = 0.18f;

    // Ignition
    [DataField]
    public float IgnitionTemperature = 800f;

    [DataField]
    public float IgnitionVolume = 50f;

    [DataField]
    public TimeSpan IgnitionDuration = TimeSpan.FromSeconds(0.5);
}
