namespace Content.Server.ADT.Atmos.Piping.Components
{
    [RegisterComponent]
    public sealed partial class PipeLeakingComponent : Component
    {
        /// <summary>
        /// Multiplier for the amount of gas transfer between the pipe and the tile when leaking.
        /// </summary>
        [DataField]
        public float LeakMultiplier = 1f;
    }
}
