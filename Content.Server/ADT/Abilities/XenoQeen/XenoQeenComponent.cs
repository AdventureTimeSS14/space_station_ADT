using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Abilities.XenoQeen
{
    /// <summary>
    /// Lets its owner entity use mime powers, like placing invisible walls.
    /// </summary>
    [RegisterComponent]
    public sealed partial class XenoQeenComponent : Component
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
    }
}
