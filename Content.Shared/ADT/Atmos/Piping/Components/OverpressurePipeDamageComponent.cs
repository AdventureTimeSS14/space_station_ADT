using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Atmos.Piping.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class OverpressurePipeDamageComponent : Component
    {
        /// <summary>
        /// Лимит давления газа в трубе.
        /// </summary>
        [DataField]
        public float LimitPressure = 0f;

        /// <summary>
        /// Максимальное значение для ломания трубы вне зависимости от давления на тайле.
        /// </summary>
        [DataField]
        public float MaxTilePressure = 0f;
    }
}