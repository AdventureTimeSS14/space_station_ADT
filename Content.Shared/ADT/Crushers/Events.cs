using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers;

public enum TrophyAlteredType : byte
{
    Inserted,
    Removed
}

[ByRefEvent]
public readonly record struct TrophyAlteredEvent(
    EntityUid Holder,
    TrophyAlteredType Alteration
);

[ByRefEvent]
public readonly record struct TrophyHolderTrophiesAlteredEvent(
    EntityUid TrophyUid,
    TrophyAlteredType Alteration
);

[ByRefEvent]
public readonly record struct CrusherMarkDetonatedEvent(
    EntityUid User,
    EntityUid Target
);

[ByRefEvent]
public readonly record struct TrophyHolderExamineEvent(FormattedMessage Message)
{
    public void PushMarkup(string markup)
    {
        var entry = new FormattedMessage();
        entry.AddMarkupOrThrow(markup);
        PushMessage(entry);
    }

    public void PushMessage(FormattedMessage entry)
    {
        if (!Message.IsEmpty)
        {
            Message.PushNewline();
            Message.PushNewline();
        }

        Message.AddMessage(entry);
    }
}

[Serializable, NetSerializable]
public sealed class ADTCrusherSpearThrowRequestEvent : EntityEventArgs
{
    public NetEntity Weapon;

    public NetCoordinates Target;

    public ADTCrusherSpearThrowRequestEvent(NetEntity weapon, NetCoordinates target)
    {
        Weapon = weapon;
        Target = target;
    }
}
