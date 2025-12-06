namespace Content.Server.Atmos.Piping.Unary.Components
{
    [RegisterComponent]
    public sealed partial class GasPassiveVentComponent : Component
    {
        [DataField("inlet")]
        public string InletName = "pipe";

        // ADT-Tweak-start
        // Multiplier for the amount of gas transfer between the pipe and the tile.
        [DataField]
        public float Multiplier = 1f;
        // ADT-Tweak-end
    }
}
