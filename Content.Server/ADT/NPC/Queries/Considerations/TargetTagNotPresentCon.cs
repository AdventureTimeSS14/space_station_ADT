using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.NPC.Queries.Considerations;

/// <summary>
/// Returns 1f if the target is alive or 0f if not.
/// </summary>
public sealed partial class TargetTagNotPresentCon : UtilityConsideration
{
    [DataField(required: true)]
    public ProtoId<TagPrototype> Tag;
}
