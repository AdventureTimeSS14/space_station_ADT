using Robust.Shared.GameStates;

namespace Content.Shared.Stealth.Components
{
    /// <summary>
    ///     When added to an entity with stealth component, this component will change the visibility
    ///     based on the entity's (lack of) movement.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    [AutoGenerateComponentState] // ADT-Tweak start
    public sealed partial class StealthOnMoveComponent : Component
    {
        /// <summary>
        /// Rate that effects how fast an entity's visibility passively changes.
        /// </summary>
        [DataField("passiveVisibilityRate")]
        public float PassiveVisibilityRate = -0.15f;

        /// <summary>
        /// Rate for movement induced visibility changes. Scales with distance moved.
        /// </summary>
        [DataField("movementVisibilityRate")]
        public float MovementVisibilityRate = 0.2f;

        // ADT-Tweak start
        [DataField]
        [AutoNetworkedField]
        public TimeSpan NoMoveTime = TimeSpan.Zero;

        // goob - break stealth on any movement
        [DataField]
        [AutoNetworkedField]
        public bool BreakOnMove;
        // ADT-Tweak end
    }
}
