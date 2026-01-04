using Content.Server.Actions;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Speech;
using Content.Shared.ADT.Shizophrenia;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Shizophrenia;

public sealed partial class SchizophreniaSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IChatManager _chatMan = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly SpeechSoundSystem _speech = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    private int _nextIdx = 1;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesBefore.Add(typeof(ActionsSystem));

        InitializeShizophrenic();
        InitializeHallucinations();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<HallucinatingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextUpdate > _timing.CurTime)
                continue;

            comp.NextUpdate = _timing.CurTime + TimeSpan.FromSeconds(0.5f);

            UpdateMusic(uid, comp);

            if (!UpdateRemoving(uid, comp))
                continue;

            UpdateEffects(uid, comp);
        }
    }

    private bool UpdateRemoving(EntityUid uid, HallucinatingComponent comp)
    {
        // Handle remove timers
        foreach (var item in new Dictionary<string, TimeSpan>(comp.Removes))
        {
            if (item.Value <= _timing.CurTime)
            {
                comp.Hallucinations.Remove(item.Key);
                comp.Removes.Remove(item.Key);
                EntityManager.RemoveComponents(uid, _proto.Index<HallucinationsPackPrototype>(item.Key).Components);

                if (!TryComp<HallucinationsMusicComponent>(uid, out var musicComp) ||
                    !musicComp.Music.ContainsKey(item.Key))
                    continue;

                musicComp.Music.Remove(item.Key);

                if (musicComp.Music.Count > 0)
                    Dirty(uid, musicComp);
                else
                    RemComp(uid, musicComp);
            }
        }

        // If there is no hallucinations, remove component
        if (comp.Hallucinations.Count <= 0)
        {
            RemComp(uid, comp);
            return false;
        }

        return true;
    }

    private void UpdateEffects(EntityUid uid, HallucinatingComponent comp)
    {
        // Hallucinate
        foreach (var item in comp.Hallucinations)
            item.Value?.TryPerform(uid, EntityManager, _random, _timing.CurTime);
    }

    private void UpdateMusic(EntityUid uid, HallucinatingComponent comp)
    {
        // Hallucinate
        foreach (var item in comp.Hallucinations)
        {
            var proto = _proto.Index<HallucinationsPackPrototype>(item.Key);
            if (proto.Music == null)
                continue;

            if (comp.Removes.TryGetValue(item.Key, out var removeTime) &&
                (removeTime - _timing.CurTime).TotalSeconds < proto.MusicDurationThreshold)
            {
                if (!TryComp<HallucinationsMusicComponent>(uid, out var musicComp) ||
                    !musicComp.Music.ContainsKey(item.Key))
                    continue;

                musicComp.Music.Remove(item.Key);

                if (musicComp.Music.Count > 0)
                    Dirty(uid, musicComp);
                else
                    RemComp(uid, musicComp);
            }
            else if (!TryComp<HallucinationsMusicComponent>(uid, out var musicComp) ||
                    !musicComp.Music.ContainsKey(item.Key))
            {
                musicComp = EnsureComp<HallucinationsMusicComponent>(uid);
                musicComp.Music.Add(item.Key, new(proto.Music, proto.MusicPlayInterval));
                Dirty(uid, musicComp);
            }
        }
    }
}
