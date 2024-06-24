[RegisterComponent]
public sealed partial class MindFlushComponent : Component
{
    /// <summary>
    /// entities mind will be flushed in that range.
    /// </summary>
    [DataField("range")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float Range { get; set; } = 7f;

}
