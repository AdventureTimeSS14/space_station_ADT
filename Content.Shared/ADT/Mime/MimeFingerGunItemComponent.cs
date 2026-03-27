using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Mime;

[RegisterComponent]
public sealed partial class MimeFingerGunItemComponent : Component
{
    [DataField("mimeUid")]
    public EntityUid? MimeUid;

    [DataField("maxShots")]
    public int MaxShots = 4;

    [DataField("remainingShots")]
    public int RemainingShots = 4;
}
