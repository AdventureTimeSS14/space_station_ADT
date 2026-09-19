using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class NewsReaderCartridgeComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public int ArticleNumber;

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public bool NotificationOn = true;

    // ADT-Tweak: кулдаун комментариев
    /// <summary>
    ///     Last time this reader posted a comment, for rate limiting.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastCommentTime;
}
