using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Mime;

[RegisterComponent]
public sealed partial class MimeFingerGunItemComponent : Component
{
    [DataField("mimeUid")]
    public EntityUid? MimeUid;
}
