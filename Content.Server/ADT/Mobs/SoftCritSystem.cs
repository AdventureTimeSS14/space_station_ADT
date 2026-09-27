using Content.Shared.ADT.Mobs;
using Content.Shared.ADT.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.ADT.Mobs;

public sealed class SoftCritSystem : SharedSoftCritSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SoftCritComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<SoftCritComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<SoftCritComponent, ComponentShutdown>(OnShutdown);
    }

    protected override void OnStateChanged(Entity<SoftCritComponent> ent, MobStateChangedEvent args)
    {
        var sound = args.NewMobState switch
        {
            MobState.SoftCritical => ent.Comp.SoftCritSound,
            MobState.Critical => ent.Comp.CritSound,
            MobState.Alive when args.OldMobState is MobState.SoftCritical or MobState.Critical => ent.Comp.ReviveSound,
            _ => null,
        };

        PlayStateSound(ent, sound);
    }

    private void OnPlayerAttached(Entity<SoftCritComponent> ent, ref PlayerAttachedEvent args)
    {
        if (!TryComp<MobStateComponent>(ent, out var mobState))
            return;

        var sound = mobState.CurrentState switch
        {
            MobState.SoftCritical => ent.Comp.SoftCritSound,
            MobState.Critical => ent.Comp.CritSound,
            _ => null,
        };

        PlayStateSound(ent, sound);
    }

    private void OnPlayerDetached(Entity<SoftCritComponent> ent, ref PlayerDetachedEvent args)
    {
        StopStateSound(ent);
    }

    private void OnShutdown(Entity<SoftCritComponent> ent, ref ComponentShutdown args)
    {
        StopStateSound(ent);
    }

    private void PlayStateSound(Entity<SoftCritComponent> ent, SoundSpecifier? sound)
    {
        StopStateSound(ent);

        if (sound == null || !TryComp<ActorComponent>(ent, out var actor))
            return;

        ent.Comp.StateAudio = _audio.PlayEntity(sound, actor.PlayerSession, ent)?.Entity;
    }

    private void StopStateSound(Entity<SoftCritComponent> ent)
    {
        ent.Comp.StateAudio = _audio.Stop(ent.Comp.StateAudio);
    }
}
