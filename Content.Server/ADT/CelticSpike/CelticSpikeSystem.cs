using Content.Server.DoAfter;
using Content.Shared.DoAfter;
using Content.Shared.ADT.Spike;
using Content.Shared.DragDrop;
using Content.Shared.Movement.Events;
using Content.Server.Popups;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Climbing.Events;



namespace Content.Server.ADT.CelticSpike
{
    public sealed class SpikeSystem : SharedSpikeSystem
    {
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedBuckleSystem _buckle = default!;
        private readonly HashSet<EntityUid> _escapeInProgress = new();
        private readonly HashSet<EntityUid> _escapeAuthorized = new();
        private readonly Dictionary<EntityUid, DoAfterId> _escapeDoAfters = new();
        private readonly HashSet<EntityUid> _buckleAuthorized = new();

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpikeComponent, DragDropTargetEvent>(OnDragDrop);
            SubscribeLocalEvent<SpikeComponent, ImpaleDoAfterEvent>(OnImpaleComplete);
            SubscribeLocalEvent<SpikeComponent, RemoveDoAfterEvent>(OnRemoveComplete);
            SubscribeLocalEvent<SpikeComponent, EscapeDoAfterEvent>(OnEscapeComplete);
            SubscribeLocalEvent<SpikeComponent, UnstrappedEvent>(OnUnstrapped);
            SubscribeLocalEvent<SpikeComponent, CanDropTargetEvent>(OnCanDropTarget);
            SubscribeLocalEvent<SpikeComponent, BuckleAttemptEvent>(OnBuckleAttempt);
            SubscribeLocalEvent<SpikeComponent, StrapAttemptEvent>(OnStrapAttempt);
            SubscribeLocalEvent<BuckleComponent, UnbuckleAttemptEvent>(OnUnbuckleAttempt);
            SubscribeLocalEvent<BuckleComponent, MoveInputEvent>(OnMoveInput);
            SubscribeLocalEvent<BuckleComponent, BuckleAttemptEvent>(OnBuckleAttemptWhileImpaled);
            SubscribeLocalEvent<BuckleComponent, AttemptClimbEvent>(OnAttemptClimbWhileImpaled);
            SubscribeLocalEvent<BuckleComponent, DragDropDraggedEvent>(OnDraggedWhileImpaled);



        }


        private void OnDragDrop(EntityUid uid, SpikeComponent component, ref DragDropTargetEvent args)
        {
            if (args.User == args.Dragged)
            {
                args.Handled = true;
                _popup.PopupEntity(Loc.GetString("celtic-spike-deny-self"), uid, args.User);
                return;
            }
            if (!HasComp<MobStateComponent>(args.Dragged))
            {
                args.Handled = true;
                _popup.PopupEntity(Loc.GetString("celtic-spike-deny-not-mob"), uid, args.User);
            }

            if (component.ImpaledEntity != null)
            {
                args.Handled = true;
                _popup.PopupEntity(Loc.GetString("celtic-spike-deny-occupied"), uid, args.User);
                return;
            }

            if (TryComp<BuckleComponent>(args.Dragged, out var draggedBuckle) && draggedBuckle.Buckled)
            {
                args.Handled = true;
                return;
            }

            StartImpaling(args.User, uid, args.Dragged);
            args.Handled = true;
        }


        private void OnUnstrapped(EntityUid uid, SpikeComponent component, ref UnstrappedEvent args)
        {
            if (component.ImpaledEntity == args.Buckle.Owner)
            {
                component.ImpaledEntity = null;
                if (_escapeDoAfters.TryGetValue(args.Buckle.Owner, out var id))
                {
                    _escapeDoAfters.Remove(args.Buckle.Owner);
                    _escapeInProgress.Remove(args.Buckle.Owner);
                    _doAfterSystem.Cancel(id);
                }
            }
        }

        private void StartImpaling(EntityUid user, EntityUid spike, EntityUid target)
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



        private void OnBuckleAttempt(EntityUid uid, SpikeComponent component, ref BuckleAttemptEvent args)
        {
            if (args.User == args.Buckle.Owner)
                args.Cancelled = true;
        }

        private void OnStrapAttempt(EntityUid uid, SpikeComponent component, ref StrapAttemptEvent args)
        {
            if (_buckleAuthorized.Contains(args.Buckle.Owner))
                return;

            args.Cancelled = true;
        }

        private void OnBuckleAttemptWhileImpaled(EntityUid uid, BuckleComponent buckle, ref BuckleAttemptEvent args)
        {

            if (!buckle.Buckled || buckle.BuckledTo == null)
                return;

            if (HasComp<SpikeComponent>(buckle.BuckledTo.Value))
                args.Cancelled = true;
        }

        private void OnCanDropTarget(EntityUid uid, SpikeComponent component, ref CanDropTargetEvent args)
        {
            if (args.User == args.Dragged)
            {
                args.CanDrop = false;
                args.Handled = true;
                _popup.PopupEntity(Loc.GetString("celtic-spike-deny-self"), uid, args.User);
            }
        }

        private void OnImpaleComplete(EntityUid uid, SpikeComponent component, ImpaleDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled || args.Target == null)
                return;

            args.Handled = true;

            _buckleAuthorized.Add(args.Target.Value);
            var buckled = _buckle.TryBuckle(args.Target.Value, args.User, uid);
            _buckleAuthorized.Remove(args.Target.Value);
            if (!buckled)
            {
                return;
            }

            component.ImpaledEntity = args.Target.Value;

            var msg = Loc.GetString("celtic-spike-impale-success", ("target", args.Target.Value));
            _popup.PopupEntity(msg, args.Target.Value);
        }

        private void OnRemoveComplete(EntityUid uid, SpikeComponent component, RemoveDoAfterEvent args)
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

        private void OnEscapeComplete(EntityUid uid, SpikeComponent component, EscapeDoAfterEvent args)
        {
            _escapeInProgress.Remove(uid);
            _escapeDoAfters.Remove(uid);

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
            if (HasComp<SpikeComponent>(args.Strap.Owner))
            {
                if (!(args.User == uid && _escapeAuthorized.Contains(uid)))
                    args.Cancelled = true;
            }
        }

        private void OnMoveInput(EntityUid uid, BuckleComponent buckle, ref MoveInputEvent args)
        {
            if (!buckle.Buckled || buckle.BuckledTo == null)
                return;

            if (!TryComp<SpikeComponent>(buckle.BuckledTo.Value, out var spikeComp))
                return;

            if (!args.HasDirectionalMovement)
                return;

            if (_escapeInProgress.Contains(uid))
                return;

            var spike = buckle.BuckledTo.Value;
            var doAfterEventArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(5), new EscapeDoAfterEvent(), spike)
            {
                BreakOnMove = false,
                BreakOnDamage = true
            };

            _escapeInProgress.Add(uid);
            if (_doAfterSystem.TryStartDoAfter(doAfterEventArgs, out var id) && id != null)
                _escapeDoAfters[uid] = id.Value;
        }

        private void OnAttemptClimbWhileImpaled(EntityUid uid, BuckleComponent buckle, ref AttemptClimbEvent args)
        {
            if (!buckle.Buckled || buckle.BuckledTo == null)
                return;

            if (!HasComp<SpikeComponent>(buckle.BuckledTo.Value))
                return;

            args.Cancelled = true;
        }

        private void OnDraggedWhileImpaled(EntityUid uid, BuckleComponent buckle, ref DragDropDraggedEvent args)
        {
            if (!buckle.Buckled || buckle.BuckledTo == null)
                return;

            if (!HasComp<SpikeComponent>(buckle.BuckledTo.Value))
                return;

            if (_escapeDoAfters.TryGetValue(uid, out var id))
            {
                _escapeDoAfters.Remove(uid);
                _escapeInProgress.Remove(uid);
                _doAfterSystem.Cancel(id);
            }
        }
    }
}
