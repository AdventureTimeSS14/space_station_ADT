[RegisterComponent]
public sealed partial class MindFlushComponent : Component
{
    /// <summary>
    /// entities mind will be flushed in that range.
    /// </summary>
    [DataField("range")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range { get; set; } = 7f;

    [DataField("duration")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan FlashDuration { get; set; } = TimeSpan.FromSeconds(10);

    [DataField("slowTo")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float SlowTo { get; set; } = 0.5f;

}
