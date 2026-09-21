using System.Numerics;
using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Systems;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Shared.ADT.Legion;
using Content.Shared.Chat;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Legion;

public sealed class ADTLegionSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private const string TargetKey = "Target";
    private const string TargetCoordinatesKey = "TargetCoordinates";

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.25);

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLegionComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        if (now < _nextUpdate)
            return;

        _nextUpdate = now + UpdateInterval;

        var query = EntityQueryEnumerator<ADTLegionComponent, HTNComponent>();

        while (query.MoveNext(out var uid, out var comp, out var htn))
        {
            if (_mobState.IsDead(uid) || HasComp<ActorComponent>(uid))
                continue;

            TrySpeak((uid, comp), now);

            if (now < comp.NextSummon)
                continue;

            if (!TryGetTarget((uid, htn), out var target))
                continue;

            comp.NextSummon = now + comp.SummonCooldown;
            SummonBrood((uid, comp), target.Value);
        }
    }

    private void SummonBrood(Entity<ADTLegionComponent> ent, EntityUid target)
    {
        var brood = Spawn(ent.Comp.BroodProto, _transform.GetMoverCoordinates(ent.Owner));

        if (TryComp<NpcFactionMemberComponent>(ent.Owner, out var ourFaction))
        {
            _faction.ClearFactions(brood, false);
            _faction.AddFactions(brood, ourFaction.Factions);
        }

        if (!TryComp<HTNComponent>(brood, out var broodHtn))
            return;

        broodHtn.Blackboard.SetValue(TargetKey, target);
        broodHtn.Blackboard.SetValue(TargetCoordinatesKey, new EntityCoordinates(target, Vector2.Zero));
        _npc.WakeNPC(brood, broodHtn);
    }

    private void TrySpeak(Entity<ADTLegionComponent> ent, TimeSpan now)
    {
        if (now < ent.Comp.NextSpeech)
            return;

        var delay = _random.Next(ent.Comp.SpeechDelayMin, ent.Comp.SpeechDelayMax);
        ent.Comp.NextSpeech = now + delay;

        if (!_random.Prob(ent.Comp.SpeechChance))
            return;

        var canSpeak = ent.Comp.SpeechLines.Count > 0;
        var canEmote = ent.Comp.EmoteLines.Count > 0;

        if (canSpeak && (!canEmote || _random.Prob(0.5f)))
        {
            var line = Loc.GetString(_random.Pick(ent.Comp.SpeechLines));
            _chat.TrySendInGameICMessage(ent.Owner, line, InGameICChatType.Speak, false);
            return;
        }

        if (!canEmote)
            return;

        var emote = Loc.GetString(_random.Pick(ent.Comp.EmoteLines));
        _chat.TrySendInGameICMessage(ent.Owner, emote, InGameICChatType.Emote, false);
    }

    public bool TryStoreBody(Entity<ADTLegionComponent> ent, EntityUid body)
    {
        var container = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.BodyContainerId);

        return _container.Insert(body, container);
    }

    private void OnMobStateChanged(Entity<ADTLegionComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var coords = _transform.GetMoverCoordinates(ent.Owner);

        _popup.PopupCoordinates(Loc.GetString(ent.Comp.DeathMessage), coords, PopupType.MediumCaution);

        var droppedBody = false;

        if (_container.TryGetContainer(ent.Owner, ent.Comp.BodyContainerId, out var container)
            && container.ContainedEntities.Count > 0)
        {
            _container.EmptyContainer(container, true, coords);
            droppedBody = true;
        }

        if (!droppedBody && ent.Comp.CorpseProto is { } corpse)
            Spawn(corpse, coords);

        if (ent.Comp.CoreProto is { } core)
            Spawn(core, coords);

        QueueDel(ent.Owner);
    }

    private bool TryGetTarget(Entity<HTNComponent> ent, [NotNullWhen(true)] out EntityUid? target)
    {
        target = null;

        if (!ent.Comp.Blackboard.TryGetValue<EntityUid>(TargetKey, out var found, EntityManager))
            return false;

        if (!found.IsValid() || TerminatingOrDeleted(found) || _mobState.IsDead(found))
            return false;

        target = found;
        return true;
    }
}
