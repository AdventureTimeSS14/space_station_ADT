//using Content.Server.Disease.Components;
//using Content.Server.Disease;

using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Felinid;
using Content.Shared.IdentityManagement;
using Content.Shared.Actions;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Linq;
/// taken from https://github.com/Workbench-Team/space-station-14/tree/arumoon-server
namespace Content.Server.Felinid
{
    /// <summary>
    /// "Lick your or other felinid wounds. Reduce bleeding, but unsanitary and can cause diseases."
    /// </summary>
    public sealed partial class WoundLickingSystem : EntitySystem
    {
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
//        [Dependency] private readonly DiseaseSystem _disease = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WoundLickingComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<WoundLickingComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<WoundLickingComponent, WoundLickingDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<WoundLickingActionEvent>(OnActionPerform);
        }

        private void OnInit(EntityUid uid, WoundLickingComponent comp, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, ref comp.WoundLickingActionEntity, comp.WoundLickingAction);
        }

        private void OnRemove(EntityUid uid, WoundLickingComponent comp, ComponentRemove args)
        {
            _actionsSystem.RemoveAction(uid, comp.WoundLickingActionEntity);
        }

        protected void OnActionPerform(WoundLickingActionEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            var performer = args.Performer;
            var target = args.Target;

            // Ensure components
            if (
                !TryComp<WoundLickingComponent>(performer, out var woundLicking) ||
                !TryComp<BloodstreamComponent>(target, out var bloodstream) ||
                !TryComp<MobStateComponent>(target, out var mobState)
            )
                return;

            // Check target
            if (mobState.CurrentState == MobState.Dead)
                return;

            // Check "CanApplyOnSelf" field
            if (performer == target & !woundLicking.CanApplyOnSelf)
            {
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-yourself-impossible"),
                    performer, Filter.Entities(performer), true);
                return;
            }

            if (woundLicking.ReagentWhitelist.Any() &&
                !woundLicking.ReagentWhitelist.Contains(bloodstream.BloodReagent)
            ) return;

            // Check bloodstream
            if (bloodstream.BleedAmount == 0)
            {
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-yourself-no-wounds"),
                    performer, performer);
                return;
            }

            // Popup


            if (target == performer)
            {
                // Applied on yourself
                var performerIdentity = Identity.Entity(performer, EntityManager);
                var otherFilter = Filter.Pvs(performer, entityManager: EntityManager)
                    .RemoveWhereAttachedEntity(e => e == performer);

                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-yourself-begin"),
                performer, performer);
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-yourself-other-begin", ("performer", performerIdentity)),
                    performer, otherFilter, true);
            }
            else
            {
                // Applied on someone else
                var targetIdentity = Identity.Entity(target, EntityManager);
                var performerIdentity = Identity.Entity(performer, EntityManager);
                var otherFilter = Filter.Pvs(performer, entityManager: EntityManager)
                    .RemoveWhereAttachedEntity(e => e == performer || e == target);

                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-performer-begin", ("target", targetIdentity)),
                performer, performer);
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-target-begin", ("performer", performerIdentity)),
                    target, target);
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-other-begin", ("performer", performerIdentity), ("target", targetIdentity)),
                    performer, otherFilter, true);
            }

            // DoAfter
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, performer, woundLicking.Delay, new WoundLickingDoAfterEvent(), performer, target: target)
            {
                BreakOnMove = true,
                BreakOnDamage = true
            });
        }

        private void OnDoAfter(EntityUid uid, WoundLickingComponent comp, WoundLickingDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled)
            {
                return;
            }
            if (TryComp<BloodstreamComponent>(args.Args.Target, out var bloodstream))
                LickWound(uid, args.Args.Target.Value, bloodstream, comp);
        }

        private void LickWound(EntityUid performer, EntityUid target, BloodstreamComponent bloodstream, WoundLickingComponent comp)
        {
            // The more you heal, the more is disease chance
            // For 15 maxHeal and 50% diseaseChance
            //  Heal 15 > chance 50%
            //  Heal 7.5 > chance 25%
            //  Heal 0 > chance 0%

            var healed = bloodstream.BleedAmount;
            if (comp.MaxHeal - bloodstream.BleedAmount < 0) healed = comp.MaxHeal;
/*            var chance = comp.DiseaseChance * (1 / comp.MaxHeal * healed);

            if (comp.DiseaseChance > 0f & comp.PossibleDiseases.Any())
            {
                if (TryComp<DiseaseCarrierComponent>(target, out var disCarrier))
                {
                    var diseaseName = comp.PossibleDiseases[_random.Next(0, comp.PossibleDiseases.Count)];
                    _disease.TryInfect(disCarrier, diseaseName, chance);
                }
            }
*/
            _bloodstreamSystem.TryModifyBleedAmount(target, -healed, bloodstream);

            if (performer == target)
            {
                // Applied on yourself
                var performerIdentity = Identity.Entity(performer, EntityManager);
                var otherFilter = Filter.Pvs(performer, entityManager: EntityManager)
                    .RemoveWhereAttachedEntity(e => e == performer);

                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-yourself-success"),
                performer, performer);
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-yourself-other-success", ("performer", performerIdentity)),
                    performer, otherFilter, true);
            }
            else
            {
                // Applied on someone else
                var targetIdentity = Identity.Entity(target, EntityManager);
                var performerIdentity = Identity.Entity(performer, EntityManager);
                var otherFilter = Filter.Pvs(performer, entityManager: EntityManager)
                    .RemoveWhereAttachedEntity(e => e == performer || e == target);

                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-performer-success", ("target", targetIdentity)),
                performer, performer);
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-target-success", ("performer", performerIdentity)),
                    target, target);
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-other-success", ("performer", performerIdentity), ("target", targetIdentity)),
                    performer, otherFilter, true);
            }
        }
    }
}
