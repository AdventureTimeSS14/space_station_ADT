using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Atmos.EntityDamage.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class OverpressurePipeDamageComponent : Component
    {
        /// <summary>
        /// Лимит давления газа в трубе.
        /// </summary>
        [DataField]
        public float LimitPressure = 0f;
    }
}