using Content.Server.ADT.Traits;
using Content.Shared.GameTicking;
using Content.Shared.Traits.Assorted;

namespace Content.Server.ADT.Speech.EntitySystems;

/// <summary>
/// Server-side handler for removing accents when the Accentless trait is applied.
/// Runs after ADTTraitSystem to ensure AccentlessComponent exists before removing accents.
/// </summary>
public sealed class AccentlessServerSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccentlessComponent, PlayerSpawnCompleteEvent>(RemoveAccentsOnSpawn,
            after: new[] { typeof(ADTTraitSystem) });
    }

    private void RemoveAccentsOnSpawn(EntityUid uid, AccentlessComponent component, PlayerSpawnCompleteEvent args)
    {
        foreach (var accent in component.RemovedAccents.Values)
        {
            var accentComponent = accent.Component;
            RemComp(uid, accentComponent.GetType());
        }
    }
}
