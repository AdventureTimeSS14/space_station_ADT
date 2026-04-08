using Content.Shared.Changeling.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
<<<<<<< HEAD
using Content.Shared.FixedPoint;
=======
>>>>>>> upstreamwiz/master
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
<<<<<<< HEAD
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
=======
>>>>>>> upstreamwiz/master

namespace Content.Shared.Changeling.Components;

/// <summary>
/// Component responsible for Changelings Devour attack. Including the amount of damage
/// and how long it takes to devour someone
/// </summary>
<<<<<<< HEAD
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
=======
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
>>>>>>> upstreamwiz/master
[Access(typeof(ChangelingDevourSystem))]
public sealed partial class ChangelingDevourComponent : Component
{
    /// <summary>
<<<<<<< HEAD
    /// The Action for devouring
=======
    /// The action for devouring.
>>>>>>> upstreamwiz/master
    /// </summary>
    [DataField]
    public EntProtoId? ChangelingDevourAction = "ActionChangelingDevour";

    /// <summary>
<<<<<<< HEAD
    /// The action entity associated with devouring
=======
    /// The action entity associated with devouring.
>>>>>>> upstreamwiz/master
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingDevourActionEntity;

    /// <summary>
<<<<<<< HEAD
    /// The whitelist of targets for devouring
=======
    /// The whitelist of targets for devouring.
>>>>>>> upstreamwiz/master
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist = new()
    {
        Components =
        [
            "MobState",
<<<<<<< HEAD
            "HumanoidAppearance",
=======
            "HumanoidProfile",
>>>>>>> upstreamwiz/master
        ],
    };

    /// <summary>
<<<<<<< HEAD
    /// The Sound to use during consumption of a victim
=======
    /// The sound to use during consumption of a victim.
>>>>>>> upstreamwiz/master
    /// </summary>
    /// <remarks>
    /// 6 distance due to the default 15 being hearable all the way across PVS. Changeling is meant to be stealthy.
    /// 6 still allows the sound to be hearable, but not across an entire department.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ConsumeNoise = new SoundCollectionSpecifier("ChangelingDevourConsume", AudioParams.Default.WithMaxDistance(6));

    /// <summary>
<<<<<<< HEAD
    /// The Sound to use during the windup before consuming a victim
=======
    /// The sound to use during the windup before consuming a victim.
>>>>>>> upstreamwiz/master
    /// </summary>
    /// <remarks>
    /// 6 distance due to the default 15 being hearable all the way across PVS. Changeling is meant to be stealthy.
    /// 6 still allows the sound to be hearable, but not across an entire department.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DevourWindupNoise = new SoundCollectionSpecifier("ChangelingDevourWindup", AudioParams.Default.WithMaxDistance(6));

    /// <summary>
<<<<<<< HEAD
    /// The time between damage ticks
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DamageTimeBetweenTicks = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The windup time before the changeling begins to engage in devouring the identity of a target
=======
    /// The windup time before the changeling begins to engage in devouring the identity of a target.
>>>>>>> upstreamwiz/master
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DevourWindupTime = TimeSpan.FromSeconds(2);

    /// <summary>
<<<<<<< HEAD
    /// The time it takes to FULLY consume someones identity.
=======
    /// The time it takes to consume someones identity.
    /// Starts after the windup.
>>>>>>> upstreamwiz/master
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DevourConsumeTime = TimeSpan.FromSeconds(10);

    /// <summary>
<<<<<<< HEAD
    /// Damage cap that a target is allowed to be caused due to IdentityConsumption
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DevourConsumeDamageCap = 350f;

    /// <summary>
    /// The Currently active devour sound in the world
=======
    /// The currently active devour sound in the world.
>>>>>>> upstreamwiz/master
    /// </summary>
    [DataField]
    public EntityUid? CurrentDevourSound;

    /// <summary>
<<<<<<< HEAD
    /// The damage profile for a single tick of devour damage
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier DamagePerTick = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
=======
    /// The damage dealt after the windup finished and devouring started.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier WindupDamage = new()
    {
        DamageDict = new()
>>>>>>> upstreamwiz/master
        {
            { "Slash", 10},
            { "Piercing", 10 },
            { "Blunt", 5 },
        },
    };

    /// <summary>
<<<<<<< HEAD
    /// The list of protective damage types capable of preventing a devour if over the threshold
=======
    /// The damage dealt after the devouring is fully finished.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier DevourDamage = new()
    {
        DamageDict = new()
        {
            { "Slash", 20},
            { "Piercing", 20 },
            { "Blunt", 10 },
        },
    };

    /// <summary>
    /// The list of protective damage types capable of preventing a devour if over the threshold.
>>>>>>> upstreamwiz/master
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<DamageTypePrototype>> ProtectiveDamageTypes = new()
    {
        "Slash",
        "Piercing",
        "Blunt",
    };

    /// <summary>
<<<<<<< HEAD
    /// The next Tick to deal damage on (utilized during the consumption "do-during" (a do after with an attempt event))
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextTick = TimeSpan.Zero;

    /// <summary>
    /// The percentage of ANY brute damage resistance that will prevent devouring
=======
    /// The percentage of ANY brute damage resistance that will prevent devouring.
>>>>>>> upstreamwiz/master
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DevourPreventionPercentageThreshold = 0.1f;

    public override bool SendOnlyToOwner => true;
}
