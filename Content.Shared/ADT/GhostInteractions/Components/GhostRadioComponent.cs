namespace Content.Shared.ADT.GhostInteractions;

/// <summary>
/// It just exists for poltergeist and phantom. I'm too lazy to make a tag (bruh)
/// </summary>
[RegisterComponent]
public sealed partial class GhostRadioComponent : Component
{
    [DataField("toggleOnInteract")]
    public bool ToggleOnInteract = true;

    [DataField("enabled")]
    public bool Enabled = true;
}
