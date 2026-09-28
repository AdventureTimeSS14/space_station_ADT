using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mech.Equipment.Systems;

/// <summary>
/// Handles everything for mech soundboard.
/// </summary>
public sealed partial class MechSoundboardSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechSoundboardComponent, MechEquipmentUiStateReadyEvent>(OnUiStateReady);
        SubscribeLocalEvent<MechSoundboardComponent, MechEquipmentUiMessageRelayEvent>(OnSoundboardMessage);
    }

    private void OnUiStateReady(EntityUid uid, MechSoundboardComponent comp, MechEquipmentUiStateReadyEvent args)
    {
        // TODO: Allocs
        var state = new MechSoundboardUiState
        {
            Sounds = new List<ProtoId<SoundCollectionPrototype>>(comp.Sounds.Count)
        };

<<<<<<< HEAD
        // ADT-Tweak-Start
        try
        {
            args.States.Add(GetNetEntity(uid), state);
        }
        catch
        {
            return;
        }
        // ADT-Tweak-End
=======
        foreach (var sound in comp.Sounds)
        {
            if (sound.Collection is { } collection)
                state.Sounds.Add(collection);
        }

        args.States.Add(GetNetEntity(uid), state);
>>>>>>> wizards-filtered
    }

    private void OnSoundboardMessage(EntityUid uid, MechSoundboardComponent comp, MechEquipmentUiMessageRelayEvent args)
    {
        if (args.Message is not MechSoundboardPlayMessage msg)
            return;

        if (!TryComp<MechEquipmentComponent>(uid, out var equipment) ||
            equipment.EquipmentOwner == null)
            return;

        if (msg.Sound >= comp.Sounds.Count)
            return;

        if (TryComp(uid, out UseDelayComponent? useDelay)
            && !_useDelay.TryResetDelay((uid, useDelay), true))
            return;

        // honk!!!!!
        _audio.PlayPredicted(comp.Sounds[msg.Sound], uid, GetEntity(args.Pilot));   // ADT Mech predict
    }
}
