using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Audio;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class RevenantComponent : Component
{
    /// <summary>
    /// The total amount of Essence the revenant has. Functions
    /// as health and is regenerated.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public FixedPoint2 Essence = 75;

    [DataField("stolenEssenceCurrencyPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<CurrencyPrototype>))]
    public string StolenEssenceCurrencyPrototype = "StolenEssence";

    /// <summary>
    /// Prototype to spawn when the entity dies.
    /// </summary>
    [DataField("spawnOnDeathPrototype", customTypeSerializer:typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string SpawnOnDeathPrototype = "Ectoplasm";

    /// <summary>
    /// The entity's current max amount of essence. Can be increased
    /// through harvesting player souls.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssence")]
    public FixedPoint2 EssenceRegenCap = 75;

    /// <summary>
    /// The coefficient of damage taken to actual health lost.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("damageToEssenceCoefficient")]
    public float DamageToEssenceCoefficient = 0.75f;

    /// <summary>
    /// The amount of essence passively generated per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("essencePerSecond")]
    public FixedPoint2 EssencePerSecond = 0.5f;

    [ViewVariables]
    public float Accumulator = 0;

    [DataField]
    public ProtoId<AlertPrototype> EssenceAlert = "Essence";

    // Here's the gist of the harvest ability:
    // Step 1: The revenant clicks on an entity to "search" for it's soul, which creates a doafter.
    // Step 2: After the doafter is completed, the soul is "found" and can be harvested.
    // Step 3: Clicking the entity again begins to harvest the soul, which causes the revenant to become vulnerable
    // Step 4: The second doafter for the harvest completes, killing the target and granting the revenant essence.
    #region Harvest Ability
    /// <summary>
    /// The duration of the soul search
    /// </summary>
    [DataField("soulSearchDuration")]
    public float SoulSearchDuration = 2.5f;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("harvestDebuffs")]
    public Vector2 HarvestDebuffs = new(5, 5);

    /// <summary>
    /// The amount that is given to the revenant each time it's max essence is upgraded.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("maxEssenceUpgradeAmount")]
    public float MaxEssenceUpgradeAmount = 10;
    #endregion

    //In the nearby radius, causes various objects to be thrown, messed with, and containers opened
    //Generally just causes a mess
    #region Defile Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileCost")]
    public FixedPoint2 DefileCost = 30;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("defileDebuffs")]
    public Vector2 DefileDebuffs = new(1, 4);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileRadius")]
    public float DefileRadius = 3.5f;

    /// <summary>
    /// The amount of tiles that are uprooted by the ability
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileTilePryAmount")]
    public int DefileTilePryAmount = 15;

    /// <summary>
    /// The chance that an individual entity will have any of the effects
    /// happen to it.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileEffectChance")]
    public float DefileEffectChance = 0.5f;

    /// <summary>
    /// Defile sound
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("defileSound")]
    public string DefileSound = "/Audio/ADT/revenant-defile.ogg";   // ADT Revenant sounds
    #endregion

    #region Overload Lights Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("overloadCost")]
    public FixedPoint2 OverloadCost = 40;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("overloadDebuffs")]
    public Vector2 OverloadDebuffs = new(3, 8);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("overloadRadius")]
    public float OverloadRadius = 5f;

    /// <summary>
    /// How close to the light the entity has to be in order to be zapped.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("overloadZapRadius")]
    public float OverloadZapRadius = 4.5f;

    /// <summary>
    /// Overload Lights sound
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("overloadSound")]
    public string OverloadSound = "/Audio/ADT/revenant-blight.ogg";   // ADT Revenant sounds
    #endregion

    #region Blight Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("blightCost")]
    public float BlightCost = 50;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("blightDebuffs")]
    public Vector2 BlightDebuffs = new(2, 5);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("blightRadius")]
    public float BlightRadius = 3.5f;
    #endregion

    #region Malfunction Ability
    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("malfunctionCost")]
    public FixedPoint2 MalfunctionCost = 60;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("malfunctionDebuffs")]
    public Vector2 MalfunctionDebuffs = new(2, 8);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("malfunctionRadius")]
    public float MalfunctionRadius = 3.5f;

    /// <summary>
    /// Whitelist for entities that can be emagged by malfunction.
    /// Used to prevent ultra gamer things like ghost emagging chem or instantly launching the shuttle.
    /// </summary>
    [DataField]
    public EntityWhitelist? MalfunctionWhitelist;

    /// <summary>
    /// Whitelist for entities that can never be emagged by malfunction.
    /// </summary>
    [DataField]
    public EntityWhitelist? MalfunctionBlacklist;

    // ADT Content start

    /// <summary>
    /// Malfunction sound
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("malfSound")]
    public string MalfSound = "/Audio/ADT/revenant-malf.ogg";
    #endregion

    #region Hysteria Ability

    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("hysteriaCost")]
    public FixedPoint2 HysteriaCost = 60;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("hysteriaDebuffs")]
    public Vector2 HysteriaDebuffs = new(2, 8);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("hysteriaRadius")]
    public float HysteriaRadius = 3.5f;

    /// <summary>
    /// How long people seeing hallucinations
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("hysteriaDuration")]
    public TimeSpan HysteriaDuration = TimeSpan.FromSeconds(35);

    /// <summary>
    /// Hallucinations prototype
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("hysteriaProto")]
    public string HysteriaProto = "Revenant";

    /// <summary>
    /// Hallucinations sound
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("hysteriaSound")]
    public string HysteriaSound = "/Audio/ADT/ghost-sing.ogg";

    #endregion

    #region Smoke Ablilty

    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("smokeCost")]
    public FixedPoint2 SmokeCost = 30;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("smokeDebuffs")]
    public Vector2 SmokeDebuffs = new(2, 7);

    /// <summary>
    /// Smoke duration
    /// </summary>
    [DataField("smokeDuration")]
    public float SmokeDuration = 20.0f;

    /// <summary>
    /// Smoke amount
    /// </summary>
    [DataField("smokeAmount")]
    public int SmokeAmount = 17;

    /// <summary>
    /// Smoke solution
    /// </summary>
    [DataField("smokeQuantity")]
    public int SmokeQuantity = 17;

    /// <summary>
    /// Smoke sound
    /// </summary>
    [DataField("smokeSound")]
    public SoundSpecifier SmokeSound = new SoundPathSpecifier("/Audio/ADT/scary-game-effect.ogg");

    #endregion

    #region Door Lock Ability

    /// <summary>
    /// The amount of essence that is needed to use the ability.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("lockCost")]
    public FixedPoint2 LockCost = 50;

    /// <summary>
    /// The status effects applied after the ability
    /// the first float corresponds to amount of time the entity is stunned.
    /// the second corresponds to the amount of time the entity is made solid.
    /// </summary>
    [DataField("lockDebuffs")]
    public Vector2 LockDebuffs = new(3, 6);

    /// <summary>
    /// The radius around the user that this ability affects
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("lockRadius")]
    public float LockRadius = 6.5f;

    /// <summary>
    /// Lock sound
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("lockSound")]
    public string LockSound = "/Audio/ADT/revenant-lock.ogg";
    #endregion
    // ADT Content end

    #region Visualizer
    [DataField("state")]
    public string State = "idle";
    [DataField("corporealState")]
    public string CorporealState = "active";
    [DataField("stunnedState")]
    public string StunnedState = "stunned";
    [DataField("harvestingState")]
    public string HarvestingState = "harvesting";
    #endregion

    [DataField] public EntityUid? Action;
}
