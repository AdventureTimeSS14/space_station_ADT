using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Abilities.XenoQueen
{
    /// <summary>
    /// Lets its owner entity use mime powers, like placing invisible walls.
    /// </summary>
    [RegisterComponent]
    public sealed partial class XenoQueenComponent : Component
    {
        /// <summary>
        /// Whether this component is active or not.
        /// </summarY>
        [DataField("enabled")]
        public bool Enabled = true;

        /// <summary>
        /// The wall prototype to use.
        /// </summary>
        [DataField("wallPrototype", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string XenoTurret = "WeaponTurretXeno";

        [DataField("xenoTurretAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string? XenoTurretAction = "ActionXenoQeenTurret";

        [DataField("xenoTurretActionEntity")] public EntityUid? XenoTurretActionEntity;
                    
        // Призывы                    
        [DataField]
        public EntityUid? ActionSpawnXenoBurrower;

        [DataField]
        public EntityUid? ActionSpawnXenoDrone;

        [DataField]
        public EntityUid? ActionSpawnXenoRunner;

        [DataField]
        public EntityUid? ActionSpawnXenoSpitter;

        [DataField]
        public EntityUid? ActionSpawnXenoPraetorian;

        [DataField]
        public EntityUid? ActionSpawnXenoRavager;
        
        [DataField]
        public EntityUid? ActionSpawnXenoQueen;
    }
}
