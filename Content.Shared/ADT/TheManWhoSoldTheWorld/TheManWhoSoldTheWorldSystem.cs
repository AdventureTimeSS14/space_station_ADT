using Content.Shared.ADT.TheManWhoSoldTheWorld.Components;
using Content.Shared.Mobs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.ADT.TheManWhoSoldTheWorld.System;

public sealed class HoloCigarSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TheManWhoSoldTheWorldComponent, MobStateChangedEvent>(OnMobStateChangedEvent);
    }

    private void OnMobStateChangedEvent(Entity<TheManWhoSoldTheWorldComponent> ent, ref MobStateChangedEvent args)
    {
        if (_net.IsServer && args.NewMobState == MobState.Dead)
            _audio.PlayPvs(ent.Comp.DeathAudio, ent, AudioParams.Default.WithVolume(3f));
    }
}
