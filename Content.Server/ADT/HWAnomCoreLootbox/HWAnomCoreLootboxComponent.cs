namespace Content.Server.ADT.HWAnomCoreLootbox
{
    /// <summary>
    ///     Spawns items when used in hand.
    /// </summary>
    [RegisterComponent]
    public sealed partial class HWAnomCoreLootboxComponent : Component
    {
        [DataField]
        public float Duration = 10f;
    }
}
