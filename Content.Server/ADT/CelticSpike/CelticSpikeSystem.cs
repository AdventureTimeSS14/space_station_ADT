using Content.Server.DoAfter;
using Content.Shared.DoAfter;
using Content.Shared.ADT.CelticSpike;
using Content.Shared.DragDrop;
using Content.Shared.Movement.Events;
using Content.Shared.Verbs;
using Content.Shared.ActionBlocker;
using Content.Server.Popups;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Alert;



namespace Content.Server.ADT.CelticSpike
{
    public sealed class CelticSpikeSystem : SharedCelticSpikeSystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedBuckleSystem _buckle = default!;
        [Dependency] private readonly AlertsSystem _alerts = default!;

        private readonly HashSet<EntityUid> _escapeInProgress = new();
        private readonly HashSet<EntityUid> _escapeAuthorized = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<CelticSpikeComponent, GetVerbsEvent<AlternativeVerb>>(AddImpaleVerb);
            SubscribeLocalEvent<CelticSpikeComponent, DragDropTargetEvent>(OnDragDrop);
            SubscribeLocalEvent<CelticSpikeComponent, ImpaleDoAfterEvent>(OnImpaleComplete);
            SubscribeLocalEvent<CelticSpikeComponent, RemoveDoAfterEvent>(OnRemoveComplete);
            SubscribeLocalEvent<CelticSpikeComponent, EscapeDoAfterEvent>(OnEscapeComplete);
            SubscribeLocalEvent<CelticSpikeComponent, StrappedEvent>(OnStrapped);
            SubscribeLocalEvent<CelticSpikeComponent, UnstrappedEvent>(OnUnstrapped);
            SubscribeLocalEvent<CelticSpikeComponent, CanDropTargetEvent>(OnCanDropTarget);
            SubscribeLocalEvent<CelticSpikeComponent, BuckleAttemptEvent>(OnBuckleAttempt);
            SubscribeLocalEvent<BuckleComponent, UnbuckleAttemptEvent>(OnUnbuckleAttempt);
            SubscribeLocalEvent<BuckleComponent, MoveInputEvent>(OnMoveInput);



        }

        private void AddImpaleVerb(EntityUid uid, CelticSpikeComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if (!args.CanInteract || !args.CanAccess)
                return;

            if (component.ImpaledEntity != null)
            {
                return;
            }

            if (!HasComp<MobStateComponent>(args.Target))
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    StartImpaling(args.User, uid, args.Target, component);
                },
                Text = Loc.GetString("celtic-spike-verb-impale"),
                Priority = 2
            };
            args.Verbs.Add(verb);
        }

        private void OnDragDrop(EntityUid uid, CelticSpikeComponent component, ref DragDropTargetEvent args)
        {
            if (!HasComp<MobStateComponent>(args.Dragged))
            {
                args.Handled = true;
                _popup.PopupEntity(Loc.GetString("celtic-spike-deny-not-mob"), uid, args.User);
            }
        }

        private void OnStrapped(EntityUid uid, CelticSpikeComponent component, ref StrappedEvent args)
        {
            if (component.ImpaledEntity != null)
                return;

            component.ImpaledEntity = args.Buckle.Owner;
            var msg = Loc.GetString("celtic-spike-impale-success", ("target", args.Buckle.Owner));
            _popup.PopupEntity(msg, args.Buckle.Owner);

            _alerts.ClearAlertCategory(args.Buckle.Owner, SharedBuckleSystem.BuckledAlertCategory);
        }

        private void OnUnstrapped(EntityUid uid, CelticSpikeComponent component, ref UnstrappedEvent args)
        {
            if (component.ImpaledEntity == args.Buckle.Owner)
                component.ImpaledEntity = null;
        }

        private void StartImpaling(EntityUid user, EntityUid spike, EntityUid target, CelticSpikeComponent component)
        {
            if (user == target)
            {
                _popup.PopupEntity(Loc.GetString("celtic-spike-deny-self"), spike, user);
                return;
            }

            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(5), new ImpaleDoAfterEvent(), spike, target: target)
            {
                BreakOnMove = true,
                BreakOnDamage = true
            };

            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        }



        private void OnBuckleAttempt(EntityUid uid, CelticSpikeComponent component, ref BuckleAttemptEvent args)
        {
            if (args.User == args.Buckle.Owner)
                args.Cancelled = true;
        }

        private void OnCanDropTarget(EntityUid uid, CelticSpikeComponent component, ref CanDropTargetEvent args)
        {
            if (args.User == args.Dragged)
            {
                args.CanDrop = false;
                args.Handled = true;
                _popup.PopupEntity(Loc.GetString("celtic-spike-deny-self"), uid, args.User);
            }
        }

        private void OnImpaleComplete(EntityUid uid, CelticSpikeComponent component, ImpaleDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || args.Target == null)
                return;

            args.Handled = true;

            if (!_buckle.TryBuckle(args.Target.Value, args.User, uid))
            {
                return;
            }

            component.ImpaledEntity = args.Target.Value;

            var msg = Loc.GetString("celtic-spike-impale-success", ("target", args.Target.Value));
            _popup.PopupEntity(msg, args.Target.Value);
        }

        private void StartRemoving(EntityUid user, EntityUid spike, CelticSpikeComponent component)
        {
            if (component.ImpaledEntity == null)
                return;

            var doAfterEventArgs = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(3), new RemoveDoAfterEvent(), spike)
            {
                BreakOnMove = true,
                BreakOnDamage = false
            };

            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        }

        private void OnRemoveComplete(EntityUid uid, CelticSpikeComponent component, RemoveDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || component.ImpaledEntity == null)
                return;

            args.Handled = true;

            var impaled = component.ImpaledEntity!.Value;
            component.ImpaledEntity = null;

            _buckle.TryUnbuckle(impaled, args.User);

            var msg = Loc.GetString("celtic-spike-remove-success", ("target", impaled));
            _popup.PopupEntity(msg, impaled);
        }

        private void OnEscapeComplete(EntityUid uid, CelticSpikeComponent component, EscapeDoAfterEvent args)
        {
            _escapeInProgress.Remove(uid);

            if (args.Cancelled || args.Handled)
                return;

            args.Handled = true;

            if (_random.Prob(component.EscapeChance))
            {
                var impaled = args.User;

                _escapeAuthorized.Add(impaled);
                _buckle.TryUnbuckle(impaled, impaled);
                _escapeAuthorized.Remove(impaled);

                component.ImpaledEntity = null;

                var msg = Loc.GetString("celtic-spike-escape-success");
                _popup.PopupEntity(msg, impaled);
            }
            else
            {
                var msg = Loc.GetString("celtic-spike-escape-failure");
                _popup.PopupEntity(msg, uid);
            }
        }

        private void OnUnbuckleAttempt(EntityUid uid, BuckleComponent buckle, ref UnbuckleAttemptEvent args)
        {
            if (HasComp<CelticSpikeComponent>(args.Strap.Owner))
            {
                if (!(args.User == uid && _escapeAuthorized.Contains(uid)))
                    args.Cancelled = true;
            }
        }

        private void OnMoveInput(EntityUid uid, BuckleComponent buckle, ref MoveInputEvent args)
        {
            if (!buckle.Buckled || buckle.BuckledTo == null)
                return;

            if (!TryComp<CelticSpikeComponent>(buckle.BuckledTo.Value, out var spikeComp))
                return;

            if (!args.HasDirectionalMovement)
                return;

            if (_escapeInProgress.Contains(buckle.BuckledTo.Value))
                return;

            _escapeInProgress.Add(buckle.BuckledTo.Value);

            var spike = buckle.BuckledTo.Value;
            var doAfterEventArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(5), new EscapeDoAfterEvent(), spike)
            {
                BreakOnMove = false,
                BreakOnDamage = true
            };

            _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        }
    }
}
