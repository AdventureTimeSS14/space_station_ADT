using System.Numerics;
using Content.Shared.ADT.Particles;
using Robust.Client.Graphics;
using Robust.Shared.Map;

namespace Content.Client.ADT.Particles;

/// <summary>
/// A running particle emitter and its live particle pool.
/// Created in <see cref="ParticleSystem"/>.
/// </summary>
public sealed class ActiveEmitter
{
    public ParticleEffectPrototype Proto = default!;

    /// <summary>
    /// How many sub-emitter links deep this emitter is. Root emitters are 0.
    /// Sub-emitters will not spawn if depth would exceed <see cref="ParticleSystem.MaxSubEmitterDepth"/>.
    /// </summary>
    public int SubEmitterDepth;

    /// <summary>Current map-space origin of the emitter, refreshed from <see cref="Coordinates"/> each frame.</summary>
    public MapCoordinates MapCoords;

    /// <summary>Grid-relative origin, so the effect follows moving and rotating grids.</summary>
    public EntityCoordinates Coordinates;

    /// <summary>
    /// Offset from the emitter origin applied to the spawn origin.
    /// Directional emitters interpret this in the attached entity's local space; other emitters in map space.
    /// </summary>
    public Vector2 SpawnOffset;

    /// <summary>Entity this emitter follows (if any).</summary>
    public EntityUid? AttachedEntity;

    /// <summary>Time elapsed since this emitter was created.</summary>
    public TimeSpan Age;

    /// <summary>Emission accumulator for sub-tick emission rates.</summary>
    public float EmitAccum;

    /// <summary>Time accumulated between quality-scaled simulation steps.</summary>
    public float SimulationAccumulator;

    /// <summary>True once the emitter stops producing new particles. Existing particles live out their lifetimes.</summary>
    public bool Exhausted;

    /// <summary>
    /// Unique client-side handle for addressing this emitter by ID.
    /// Prefer holding the <see cref="ActiveEmitter"/> reference directly when possible, please.
    /// </summary>
    public uint Handle;

    /// <summary>Color tint multiplied on top of every particle's computed color.</summary>
    public Color? ColorOverride;

    /// <summary>Intensity multiplier for emission rate and particle size. 1.0 = normal.</summary>
    public float Intensity = 1f;

    /// <summary>
    /// Live overrides shadowing individual prototype fields.
    /// Non-null values take priority, null falls back to the prototype.
    /// </summary>
    public ParticleRuntimeOverrides? Overrides;

    /// <summary>Pre-sampled prototype curves shared by emitters of the same effect.</summary>
    internal ParticleCurveCache Curves = null!;

    // =^..^= Velocity tracking =^..^=

    public Vector2 PreviousPosition;
    public Vector2 EmitterVelocity;
    public bool VelocityInitialized;

    // =^..^= Aim-at targeting =^..^=

    /// <summary>
    /// When set, each tick the emit angle is recomputed to point toward this entity.
    /// Falls back to <see cref="TargetPosition"/> if the entity is deleted.
    /// </summary>
    public EntityUid? TargetEntity;

    /// <summary>
    /// When set, the emit angle points toward this world position.
    /// Used as a fallback when <see cref="TargetEntity"/> is unset or gone.
    /// </summary>
    public Vector2? TargetPosition;

    /// <summary>Resolved emit angle in radians, recomputed each tick from the target if one is set.</summary>
    public float EffectiveEmitAngle;

    // =^..^= Timed bursts =^..^=

    /// <summary>Tracks which <see cref="ParticleEffectPrototype.Bursts"/> entries have already fired.</summary>
    public readonly List<bool> FiredBursts = new();

    // =^..^= Animation =^..^=

    /// <summary>Resolved RSI frames. Populated on creation.
    /// Single-frame sprites have one entry and empty Delays.</summary>
    public Texture[] Frames = Array.Empty<Texture>();

    /// <summary>Frame delays when an RSI defines animation.</summary>
    public float[] Delays = Array.Empty<float>();

    public int AnimFrame;
    public float AnimTimer;

    // =^..^= Particles =^..^=

    // ParticleData objects are never removed from Particles once added.
    // Live particles are kept densely packed at the front of the list ([0, LiveTail)).
    // When a particle dies, KillParticle swaps it with the last live particle and decrements
    // LiveTail, parking the dead slot at the tail for reuse. The next emission takes a slot
    // from the tail (if any) or appends a new one, so no per-particle allocation happens
    // after warmup and the simulation loop only ever iterates live slots.

    /// <summary>
    /// All particle slots. Live particles occupy indices [0, <see cref="LiveTail"/>);
    /// dead slots are parked at [<see cref="LiveTail"/>, Count) and reused by EmitParticle.
    /// </summary>
    public readonly List<ParticleData> Particles = new();

    /// <summary>
    /// Number of live particles at the front of <see cref="Particles"/>. Invariant:
    /// every index in [0, LiveTail) is alive, every index in [LiveTail, Count) is dead.
    /// Kept in sync by <see cref="ParticleSystem.KillParticle"/> and EmitParticle.
    /// </summary>
    public int LiveTail;

    /// <summary>
    /// Distance-based priority recomputed periodically by <see cref="ParticleSystem.FrameUpdate"/>.
    /// Nearest in-view emitters get first pick of the global particle budget; exhausted and
    /// other-map emitters are scheduled last.
    /// </summary>
    public float FramePriority;
}