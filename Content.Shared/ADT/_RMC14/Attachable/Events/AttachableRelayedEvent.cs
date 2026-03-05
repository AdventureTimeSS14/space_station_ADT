using Content.Shared.Hands;

namespace Content.Shared._RMC14.Attachable.Events;

/// <summary>
/// Wrapper for events relayed to attachables by their holder.
/// </summary>
public sealed class AttachableRelayedEvent<TEvent> : EntityEventArgs
{
    public TEvent Args;
    public EntityUid Holder;
    public EntityUid User;

    public AttachableRelayedEvent(TEvent args, EntityUid holder, EntityUid user = default)
    {
        Args = args;
        Holder = holder;
        
        if (args is EquippedHandEvent equippedEvent)
            User = equippedEvent.User;
        else if (args is UnequippedHandEvent unequippedEvent)
            User = unequippedEvent.User;
        else
            User = user;
    }
}
