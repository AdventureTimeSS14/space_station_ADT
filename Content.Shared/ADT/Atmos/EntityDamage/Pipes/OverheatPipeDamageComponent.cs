using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Atmos.EntityDamage.Pipes
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class OverheatPipeDamageComponent : Component
    {        
        /// <summary>
        /// Our limit temperature that we can have in pipe.  
        /// </summary>
        [DataField]
        public float LimitTemperature = 0f;
    }
}