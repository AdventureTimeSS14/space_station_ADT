using Content.Server.Objectives.Components;
using Content.Server.Popups;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.ADT.Pinpointer;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Pinpointer.Systems;

public sealed class ObjectivePinpointerSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPinpointerSystem _pinpointer = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ObjectivePinpointerComponent, GetVerbsEvent<ActivationVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ObjectivePinpointerComponent, ObjectivePinpointerSelectMessage>(OnSelectTarget);
    }

    private void OnGetVerbs(EntityUid uid, ObjectivePinpointerComponent comp, GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        // Set owner if not set
        if (comp.OwnerMind == null && _mind.TryGetMind(user, out var mindId, out _))
        {
            comp.OwnerMind = mindId;
        }

        args.Verbs.Add(new ActivationVerb
        {
            Text = Loc.GetString("objective-pinpointer-verb-select"),
            Act = () => OpenUI(uid, comp, user)
        });
    }

    private void OpenUI(EntityUid uid, ObjectivePinpointerComponent comp, EntityUid user)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        if (comp.OwnerMind == null || !TryComp<MindComponent>(comp.OwnerMind, out var mind))
        {
            _popup.PopupEntity(Loc.GetString("objective-pinpointer-no-objectives"), uid, user);
            return;
        }

        var targets = GetAvailableTargets(mind);
        if (targets.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("objective-pinpointer-no-objectives"), uid, user);
            return;
        }

        _ui.OpenUi(uid, ObjectivePinpointerUiKey.Key, actor.PlayerSession);
        _ui.SetUiState(uid, ObjectivePinpointerUiKey.Key, new ObjectivePinpointerBoundUserInterfaceState(targets));
    }

    private List<ObjectivePinpointerTarget> GetAvailableTargets(MindComponent mind)
    {
        var targets = new List<ObjectivePinpointerTarget>();
        var addedEntities = new HashSet<EntityUid>();

        foreach (var objective in mind.Objectives)
        {
            // Check for steal objectives
            if (TryComp<StealConditionComponent>(objective, out var steal))
            {
                var stealCandidates = new List<(EntityUid Uid, StealTargetComponent Comp)>();
                var query = AllEntityQuery<StealTargetComponent>();
                while (query.MoveNext(out var targetUid, out var stealTarget))
                {
                    if (stealTarget.StealGroup == steal.StealGroup)
                    {
                        stealCandidates.Add((targetUid, stealTarget));
                    }
                }

                foreach (var (targetUid, _) in stealCandidates)
                {
                    if (addedEntities.Contains(targetUid))
                        continue;

                    var name = Name(targetUid);
                    targets.Add(new ObjectivePinpointerTarget(
                        GetNetEntity(targetUid),
                        name,
                        ObjectivePinpointerTargetType.Steal
                    ));
                    addedEntities.Add(targetUid);
                }
            }

            // Check for kill objectives
            if (TryComp<KillPersonConditionComponent>(objective, out _) &&
                TryComp<TargetObjectiveComponent>(objective, out var killTarget) &&
                killTarget.Target != null)
            {
                if (TryComp<MindComponent>(killTarget.Target.Value, out var targetMind) &&
                    targetMind.OwnedEntity != null)
                {
                    var targetEntity = targetMind.OwnedEntity.Value;
                    if (addedEntities.Contains(targetEntity))
                        continue;

                    var name = Name(targetEntity);
                    targets.Add(new ObjectivePinpointerTarget(
                        GetNetEntity(targetEntity),
                        name,
                        ObjectivePinpointerTargetType.Kill
                    ));
                    addedEntities.Add(targetEntity);
                }
            }

            // Check for keep-alive/protect objectives
            if (TryComp<KeepAliveConditionComponent>(objective, out _) &&
                TryComp<TargetObjectiveComponent>(objective, out var protectTarget) &&
                protectTarget.Target != null)
            {
                if (TryComp<MindComponent>(protectTarget.Target.Value, out var targetMind) &&
                    targetMind.OwnedEntity != null)
                {
                    var targetEntity = targetMind.OwnedEntity.Value;

                    if (addedEntities.Contains(targetEntity))
                        continue;

                    var name = Name(targetEntity);
                    targets.Add(new ObjectivePinpointerTarget(
                        GetNetEntity(targetEntity),
                        name,
                        ObjectivePinpointerTargetType.Protect
                    ));
                    addedEntities.Add(targetEntity);
                }
            }
        }

        return targets;
    }

    private void OnSelectTarget(EntityUid uid, ObjectivePinpointerComponent comp, ObjectivePinpointerSelectMessage args)
    {
        if (!TryComp<PinpointerComponent>(uid, out var pinpointer))
            return;

        var targetEntity = GetEntity(args.Target);

        _pinpointer.SetTarget(uid, targetEntity, pinpointer);

        var targetName = Name(targetEntity);
        _popup.PopupEntity(
            Loc.GetString("objective-pinpointer-target-set", ("target", targetName)),
            uid,
            args.Actor
        );

        _ui.CloseUi(uid, ObjectivePinpointerUiKey.Key, args.Actor);
    }
}