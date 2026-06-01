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
        [DataField("limitPressure")]
        public float LimitPressure = 0f;

        /// <summary>
        /// Базовый множитель урона. Формула: damage = baseDamage * exp(overPressure / limitPressure).
        /// </summary>
        [DataField("baseDamage")]
        public float BaseDamage = 10f;

        /// <summary>
        /// Базовый шанс нанесения урона (от 0 до 1)
        /// Реальный шанс увеличивается с накопленным уроном
        /// </summary>
        [DataField("baseChance")]
        public float BaseChance = 0.5f;

        /// <summary>
        /// КД между нанесениями урона в секундах
        /// </summary>
        [DataField("cooldown")]
        public float Cooldown = 1.0f;

        /// <summary>
        /// Время последнего нанесения урона
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float LastDamageTime = 0f;
    }
}
