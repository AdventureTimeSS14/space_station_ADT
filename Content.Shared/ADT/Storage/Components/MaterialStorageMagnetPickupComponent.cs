namespace Content.Shared.ADT.Storage.Components;

/// <summary>
/// Applies an ongoing pickup area around the attached entity.
/// </summary>
[RegisterComponent]
public sealed partial class MaterialStorageMagnetPickupComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("nextScan")]
    public TimeSpan NextScan = TimeSpan.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField("range")]
    public float Range = 1f;

    /// <summary>
    /// Is the magnet currently enabled?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("magnetEnabled")]
    public bool MagnetEnabled = false;

    /// <summary>
    /// Do we need the magnet to be switched?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("magnetToggle")]
    public bool MagnetToggle = true;

}
