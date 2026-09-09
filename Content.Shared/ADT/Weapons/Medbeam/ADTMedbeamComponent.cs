using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Explosion;
using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Weapons.Medbeam;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTMedbeamComponent : Component
{
    /// <summary>
    ///     The entity currently being healed by the beam.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    /// <summary>
    ///     Whether the gun only works while installed inside a mech.
    /// </summary>
    [DataField]
    public bool RequireMech;

    /// <summary>
    ///     How much mech energy the beam drains per second while healing.
    /// </summary>
    [DataField]
    public float EnergyUsage;

    /// <summary>
    ///     How far the beam can stretch before it breaks.
    /// </summary>
    [DataField]
    public float MaxRange = 8f;

    /// <summary>
    ///     How often the beam heals the target.
    /// </summary>
    [DataField]
    public float UpdateInterval = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Accumulator;

    /// <summary>
    ///     Healing applied per tick. Negative damage heals.
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new();

    /// <summary>
    ///     How much blood is restored per tick.
    /// </summary>
    [DataField]
    public float BloodRestore = 5f;

    /// <summary>
    ///     Explosion triggered when two beams cross.
    /// </summary>
    [DataField]
    public ProtoId<ExplosionPrototype> ExplosionType = SharedExplosionSystem.DefaultExplosionPrototypeId;

    [DataField]
    public float ExplosionTotalIntensity = 100f;

    [DataField]
    public float ExplosionIntensitySlope = 4f;

    [DataField]
    public float ExplosionMaxTileIntensity = 10f;

    [DataField]
    public SpriteSpecifier Beam =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/ADT/Misc/medbeam.rsi"), "medbeam");

    [DataField]
    public SpriteSpecifier? Start;

    [DataField]
    public SpriteSpecifier? End;

    [DataField]
    public Vector2 Scale = Vector2.One;
}