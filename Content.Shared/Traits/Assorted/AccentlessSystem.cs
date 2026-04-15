using Content.Shared.GameTicking;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This handles removing accents when using the accentless trait.
/// </summary>
public sealed class AccentlessSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccentlessComponent, PlayerSpawnCompleteEvent>(RemoveAccentsOnSpawn); // ADT-Tweak
        SubscribeLocalEvent<AccentlessComponent, ComponentStartup>(RemoveAccents);
    }

    // ADT-Tweak start
    private void RemoveAccentsOnSpawn(EntityUid uid, AccentlessComponent component, PlayerSpawnCompleteEvent args)
    {
        RemoveAccentsInternal(uid, component);
    }
    // ADT-Tweak end

    private void RemoveAccents(EntityUid uid, AccentlessComponent component, ComponentStartup args)
    // ADT-Tweak start
    {
        RemoveAccentsInternal(uid, component);
    }
    // ADT-Tweak end

    private void RemoveAccentsInternal(EntityUid uid, AccentlessComponent component) // ADT-Tweak
    {
        foreach (var accent in component.RemovedAccents.Values)
        {
            var accentComponent = accent.Component;
            RemComp(uid, accentComponent.GetType());
        }
    }
}
