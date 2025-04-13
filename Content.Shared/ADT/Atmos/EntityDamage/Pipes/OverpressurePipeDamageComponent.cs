using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Atmos.EntityDamage.Pipes
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class OverpressurePipeDamageComponent : Component
    {        
        /// <summary>
        /// Our limit pressure that we can have in pipe.  
        /// </summary>
        [DataField]
        public float LimitPressure = 6500f;
    }
}