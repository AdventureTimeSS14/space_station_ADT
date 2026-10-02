using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Shared.Actions;
using Content.Shared.ADT.LinkedBodies;
using Content.Shared.Database;
using Robust.Server.GameStates;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.LinkedBodies;

public sealed class ADTLinkedBodiesSystem : EntitySystem
{
    private static readonly EntProtoId SwapAction = "ADTActionLinkedBodySwap";

    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLinkedBodiesComponent, ADTLinkedBodySwapActionEvent>(OnSwap);
        SubscribeLocalEvent<ADTLinkedBodiesComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ADTLinkedBodiesComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ADTLinkedBodiesComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }

    public void Link(IEnumerable<EntityUid> bodies)
    {
        var group = new HashSet<EntityUid>();
        foreach (var body in bodies)
        {
            group.Add(body);

            if (TryComp<ADTLinkedBodiesComponent>(body, out var linked))
                group.UnionWith(linked.Actions.Keys);
        }

        foreach (var body in group)
        {
            var comp = EnsureComp<ADTLinkedBodiesComponent>(body);
            foreach (var other in group)
            {
                if (other != body && !comp.Actions.ContainsKey(other))
                    AddSwapAction((body, comp), other);
            }
        }
    }

    public void Unlink(EntityUid body)
    {
        RemComp<ADTLinkedBodiesComponent>(body);
    }

    private void AddSwapAction(Entity<ADTLinkedBodiesComponent> ent, EntityUid target)
    {
        EntityUid? action = null;
        if (!_actions.AddAction(ent, ref action, SwapAction))
            return;

        _actions.SetEntityIcon(action.Value, target);
        _metaData.SetEntityName(action.Value, Loc.GetString("adt-linked-bodies-action-name", ("target", target)));
        ent.Comp.Actions[target] = action.Value;

        if (TryComp<ActorComponent>(ent, out var actor))
            _pvsOverride.AddSessionOverride(target, actor.PlayerSession);
    }

    private void OnSwap(Entity<ADTLinkedBodiesComponent> ent, ref ADTLinkedBodySwapActionEvent args)
    {
        if (args.Handled)
            return;

        if (GetTarget(ent, args.Action) is not { } targetUid || TerminatingOrDeleted(targetUid))
            return;

        if (!_mind.TryGetMind(args.Performer, out var mindId, out var mind))
            return;

        args.Handled = true;

        var targetHasMind = _mind.TryGetMind(targetUid, out var targetMindId, out _);

        _audio.PlayPvs(ent.Comp.SwapSound, args.Performer);
        _mind.TransferTo(mindId, targetUid, mind: mind);

        if (targetHasMind)
            _mind.TransferTo(targetMindId, args.Performer);

        _audio.PlayPvs(ent.Comp.SwapSound, targetUid);
        _adminLog.Add(LogType.Mind, LogImpact.Low, $"{ToPrettyString(args.Performer):player} moved into linked body {ToPrettyString(targetUid):target}");
    }

    private static EntityUid? GetTarget(Entity<ADTLinkedBodiesComponent> ent, EntityUid action)
    {
        foreach (var (target, targetAction) in ent.Comp.Actions)
        {
            if (targetAction == action)
                return target;
        }

        return null;
    }

    private void OnShutdown(Entity<ADTLinkedBodiesComponent> ent, ref ComponentShutdown args)
    {
        var session = CompOrNull<ActorComponent>(ent)?.PlayerSession;

        foreach (var (other, action) in ent.Comp.Actions)
        {
            if (session != null)
                _pvsOverride.RemoveSessionOverride(other, session);

            if (!TerminatingOrDeleted(ent))
            {
                _actions.RemoveAction(ent.Owner, action);
                QueueDel(action);
            }

            if (!TryComp<ADTLinkedBodiesComponent>(other, out var otherComp) ||
                !otherComp.Actions.Remove(ent.Owner, out var otherAction))
            {
                continue;
            }

            if (TryComp<ActorComponent>(other, out var otherActor))
                _pvsOverride.RemoveSessionOverride(ent, otherActor.PlayerSession);

            _actions.RemoveAction(other, otherAction);
            QueueDel(otherAction);

            if (otherComp.Actions.Count == 0)
                RemCompDeferred<ADTLinkedBodiesComponent>(other);
        }

        ent.Comp.Actions.Clear();
    }

    private void OnPlayerAttached(Entity<ADTLinkedBodiesComponent> ent, ref PlayerAttachedEvent args)
    {
        foreach (var other in ent.Comp.Actions.Keys)
        {
            _pvsOverride.AddSessionOverride(other, args.Player);
        }
    }

    private void OnPlayerDetached(Entity<ADTLinkedBodiesComponent> ent, ref PlayerDetachedEvent args)
    {
        foreach (var other in ent.Comp.Actions.Keys)
        {
            _pvsOverride.RemoveSessionOverride(other, args.Player);
        }
    }
}
