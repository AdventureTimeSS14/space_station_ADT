// ADT-Geras-Tweak-Start
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;
// ADT-Geras-Tweak-End

namespace Content.Shared.Polymorph;

/// <summary>
/// Raised locally on an entity when it polymorphs into another entity
/// </summary>
/// <param name="OldEntity">EntityUid of the entity before the polymorph</param>
/// <param name="NewEntity">EntityUid of the entity after the polymorph</param>
/// <param name="IsRevert">Whether this polymorph event was a revert back to the original entity</param>
[ByRefEvent]
public record struct PolymorphedEvent(EntityUid OldEntity, EntityUid NewEntity, bool IsRevert);

// ADT-Geras-Tweak-Start
[Serializable, NetSerializable]
public sealed partial class RevertPolymorphDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
// ADT-Geras-Tweak-End
