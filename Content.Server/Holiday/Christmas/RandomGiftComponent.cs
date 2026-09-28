using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Holiday.Christmas;

/// <summary>
/// This is used for gifts with COMPLETELY random things.
/// </summary>
[RegisterComponent, Access(typeof(RandomGiftSystem))]
public sealed partial class RandomGiftComponent : Component
{
    /// <summary>
    /// The wrapper entity to spawn when unwrapping the gift.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Wrapper;

    /// <summary>
    ///     A sound to play when the items are spawned. For example, gift boxes being unwrapped.
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Whether or not the gift should be limited only to actual items.
    /// </summary>
<<<<<<< HEAD
    [DataField("insaneMode"), ViewVariables(VVAccess.ReadWrite)] // По умолчанию тип bool с required: true
    public string? InsaneMode;
=======
    [DataField(required: true)]
    public bool InsaneMode;
>>>>>>> wizards-filtered

    /// <summary>
    /// What entities are allowed to examine this gift to see its contents.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist ContentsViewers = default!;

    /// <summary>
    /// The currently selected entity to give out. Used so contents viewers can see inside.
    /// </summary>
    [DataField]
    public string? SelectedEntity;
}
