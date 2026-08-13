using Content.Shared.Chat;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.Inventory.Events;
using Content.Shared.Stunnable;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.ADT.Chat;

public sealed partial class TypingSoundSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TypingSoundComponent, TypingIndicatorStateChangedEvent>(OnTypingStateChanged);
        SubscribeLocalEvent<TypingSoundComponent, EntitySpokeEvent>(OnEntitySpoke);
        SubscribeLocalEvent<TypingSoundComponent, GotEquippedEvent>(GotEquipped);
        SubscribeLocalEvent<TypingSoundComponent, GotUnequippedEvent>(GotUnequipped);
    }

    private void OnTypingStateChanged(EntityUid uid, TypingSoundComponent component, TypingIndicatorStateChangedEvent args)
    {
        if (args.NewState != TypingIndicatorState.Typing)
            return;

        _audio.PlayPvs(component.TypingSound, uid);
    }

    private void OnEntitySpoke(EntityUid uid, TypingSoundComponent component, EntitySpokeEvent args)
    {
        _audio.PlayPvs(component.MessageSentSound, uid);
    }

    private void GotEquipped(Entity<TypingSoundComponent> ent, ref GotEquippedEvent args)
    {
        EnsureComp<TypingSoundComponent>(args.Equipee);
    }

    private void GotUnequipped(Entity<TypingSoundComponent> ent, ref GotUnequippedEvent args)
    {
        RemCompDeferred<TypingSoundComponent>(args.Equipee);
    }
}
