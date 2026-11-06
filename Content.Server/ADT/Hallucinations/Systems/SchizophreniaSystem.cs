using System.Linq;
using Content.Server.Actions;
using Content.Server.ADT.Hallucinations.Components;
using Content.Server.Popups;
using Content.Shared.ADT.Hallucinations.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Hallucinations.Systems;

public sealed partial class SchizophreniaSystem : EntitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private VisibilitySystem _visibility = default!;
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private PopupSystem _popup = default!;

    [Dependency] private EntityQuery<SchizophreniaComponent> _schizQuery;
    [Dependency] private EntityQuery<HallucinationComponent> _hallucinationQuery;

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
        foreach (var item in comp.Removes.ToDictionary())
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
            RemCompDeferred(uid, comp);
            return false;
        }

        return true;
    }

    private void UpdateEffects(EntityUid uid, HallucinatingComponent comp)
    {
        // Hallucinate
        foreach (var (_, hallucinations) in comp.Hallucinations)
        {
            if (hallucinations.Count <= 0)
                continue;

            foreach (var compound in hallucinations)
            {
                if (compound.PerformTime > _timing.CurTime)
                    continue;

                Perform(uid, compound.Type);
                compound.PerformTime = _timing.CurTime + TimeSpan.FromSeconds(compound.Type.Delay.Next(_random));
            }
        }
    }

    private void UpdateMusic(EntityUid uid, HallucinatingComponent comp)
    {
        // Hallucinate
        foreach (var (id, _) in comp.Hallucinations)
        {
            var proto = _proto.Index<HallucinationsPackPrototype>(id);
            if (proto.Music == null)
                continue;

            if (comp.Removes.TryGetValue(id, out var removeTime) &&
                (removeTime - _timing.CurTime).TotalSeconds < proto.MusicDurationThreshold)
            {
                if (!TryComp<HallucinationsMusicComponent>(uid, out var musicComp) ||
                    !musicComp.Music.ContainsKey(id))
                    continue;

                musicComp.Music.Remove(id);

                if (musicComp.Music.Count > 0)
                    Dirty(uid, musicComp);
                else
                    RemComp(uid, musicComp);
            }
            else if (!TryComp<HallucinationsMusicComponent>(uid, out var musicComp) ||
                    !musicComp.Music.ContainsKey(id))
            {
                musicComp = EnsureComp<HallucinationsMusicComponent>(uid);
                musicComp.Music.Add(id, new(proto.Music, proto.MusicPlayInterval));
                Dirty(uid, musicComp);
            }
        }
    }
}
