using Content.Shared.Chemistry.Components;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Shared.Damage; // ADT-Tweak

namespace Content.Server.Dragon
{
    // TODO: use timespans for logic
    [RegisterComponent]
    public sealed partial class DragonComponent : Component
    {
        /// <summary>
        /// If we have active rifts.
        /// </summary>
        [DataField]
        public List<EntityUid> Rifts = new();

        public bool Weakened => WeakenedAccumulator > 0f;

        /// <summary>
        /// When any rift is destroyed how long is the dragon weakened for
        /// </summary>
        [DataField]
        public float WeakenedDuration = 120f;

        /// <summary>
        /// Has a rift been destroyed and the dragon in a temporary weakened state?
        /// </summary>
        [DataField]
        public float WeakenedAccumulator = 0f;

        [DataField]
        public float RiftAccumulator = 0f;

        /// <summary>
        /// Maximum time the dragon can go without spawning a rift before they die.
        /// </summary>
        [DataField]
        public float RiftMaxAccumulator = 300f;

        [DataField]
        public EntProtoId SpawnRiftAction = "ActionSpawnRift";

        /// <summary>
        /// Spawns a rift which can summon more mobs.
        /// </summary>
        [DataField]
        public EntityUid? SpawnRiftActionEntity;

        [DataField]
        public EntProtoId RiftPrototype = "CarpRift";

        [DataField]
        public SoundSpecifier? SoundDeath = new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg");

        [DataField]
        public SoundSpecifier? SoundRoar =
            new SoundPathSpecifier("/Audio/Animals/space_dragon_roar.ogg")
            {
                Params = AudioParams.Default.AddVolume(3f),
            };

        /// <summary>
        /// NPC faction to re-add after being zombified.
        /// Prevents zombie dragon from being attacked by its own carp.
        /// </summary>
        [DataField]
        public ProtoId<NpcFactionPrototype> Faction = "Dragon";

        /// <summary>
        /// The smoke to spawn upon rift timeout death.
        /// </summary>
        [DataField]
        public EntProtoId SmokePrototype = "BloodSmoke";

        /// <summary>
        /// The solution to place into the smoke (mostly just needed for color)
        /// </summary>
        [DataField]
        public Solution SmokeSolution = new ([new("Blood", 1)]);

        // ADT-Tweak-start
        [DataField]
        public EntityUid? SpawnCarpsActionEntity;

        [DataField]
        public EntProtoId SpawnCarpsAction = "ActionRiseFish";

        [DataField]
        public EntProtoId CarpProtoId = "MobCarpDragon";

        [DataField]
        public int CarpAmount = 3;

        [DataField]
        public EntityUid? RoarActionEntity;

        [DataField]
        public EntProtoId RoarAction = "ActionDragonRoar";

        [DataField]
        public float RoarRange = 3f;

        [DataField]
        public float RoarStunTime = 2.5f; // ADT-tweak (3 ---> 2.5)

        [DataField]
        public float CarpRiftHealingRange = 7f;

        /// <summary>
        /// Amount of healing the dragon receives when standing near a carp rift per second.
        /// </summary>
        [DataField]
        public DamageSpecifier CarpRiftHealing = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        public float RiftHealTimer = 0f;
        // ADT-Tweak-end
    }
}
