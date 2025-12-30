using System.Reflection.Metadata;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Part;
using Content.Shared.Bible.Components;
using Content.Shared.DoAfter;
using Content.Shared.Cargo;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.ADT.HWAnomCoreLootbox;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Traits.Assorted;
using Content.Shared.StatusEffect;
using static Content.Shared.Storage.EntitySpawnCollection;
using Content.Shared.Verbs;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.ADT.HWAnomCoreLootbox
{
    public sealed class HWAnomCoreLootboxSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly StatusEffectsSystem _status = default!;
        [Dependency] private readonly SharedBodySystem _bodySystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HWAnomCoreLootboxComponent, UseInHandEvent>(OnUseInHand);
            SubscribeLocalEvent<HWAnomCoreLootboxComponent, HWAnomCoreLootboxDoAfterEvent>(OnDoAfter);
        }
        private void OnUseInHand(EntityUid uid, HWAnomCoreLootboxComponent component, UseInHandEvent args)
        {
            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, component.Settings.UseDelay, new HWAnomCoreLootboxDoAfterEvent(), uid)
            {
                BreakOnDamage = true,
                BreakOnMove = false,
                BreakOnHandChange = true,
                AttemptFrequency = AttemptFrequency.EveryTick,
                CancelDuplicate = true,
                BlockDuplicate = true
            };
            _doAfterSystem.TryStartDoAfter(doAfterArgs, out component.DoAfter);
        }
        private void OnDoAfter(Entity<HWAnomCoreLootboxComponent> ent, ref HWAnomCoreLootboxDoAfterEvent args)
        {
            _audio.PlayPvs(ent.Comp.DoAfterSound, ent);
            float choosenumber = _random.NextFloat(0f, 11f);
            switch (choosenumber)
            {
                case > 0f and < 4f:
                    ChaplainDo(args.User);
                    QueueDel(ent);
                    break;
                case > 3f and < 6f:
                    Blind(ent, args.User);
                    QueueDel(ent);
                    break;
                case > 6f and < 8f:
                    Hemophilia(args.User);
                    QueueDel(ent);
                    break;
                case > 8f and < 10f:
                    Paracusia(args.User);
                    QueueDel(ent);
                    break;
                case 10f:
                    Damageplayer(args.User);
                    QueueDel(ent);
                    break;

            }
        }
        private void ChaplainDo(EntityUid uid)
        {
            if (!HasComp<ChaplainComponent>(uid))
            {
                EnsureComp<ChaplainComponent>(uid);
                var msg = Loc.GetString("anombook-church-popup",("player", uid));
                _popupSystem.PopupEntity(msg, uid, uid);
            }
            else
            {
                var msg = Loc.GetString("anombook-nothing-heppend",("player", uid));
                _popupSystem.PopupEntity(msg, uid, uid);
            }
        }

        private void Blind(HWAnomCoreLootboxComponent comp, EntityUid user)
        {
            if (!HasComp<HWAnomCoreLootboxComponent>(comp.Owner))
                return;
            var msg = Loc.GetString("anombook-blind-popup",("player", user));
            _popupSystem.PopupEntity(msg, user, user);
            _status.TryAddStatusEffect<TemporaryBlindnessComponent>(user, TemporaryBlindnessSystem.BlindingStatusEffect, TimeSpan.FromSeconds(comp.Settings.Duration), true);
        }

        private void Hemophilia(EntityUid uid)
        {
            if (!HasComp<HemophiliaComponent>(uid))
            {
                EnsureComp<HemophiliaComponent>(uid);
                var msg = Loc.GetString("anombook-gemophilia-popup",("player", uid));
                _popupSystem.PopupEntity(msg, uid, uid);
            }
            else
            {
                var msg = Loc.GetString("anombook-nothing-heppend",("player", uid));
                _popupSystem.PopupEntity(msg, uid, uid);
            }
        }

        private void Paracusia(EntityUid uid)
        {
            if (!HasComp<ParacusiaComponent>(uid))
            {
                EnsureComp<ParacusiaComponent>(uid);
                var msg = Loc.GetString("anombook-parakuzia-popup",("player", uid));
                _popupSystem.PopupEntity(msg, uid, uid);
            }
            else
            {
                var msg = Loc.GetString("anombook-nothing-heppend",("player", uid));
                _popupSystem.PopupEntity(msg, uid, uid);
            }
        }

        private void Damageplayer(EntityUid uid)
        {
            var baseXform = Transform(uid);

            Timer.Spawn(0, () =>
            {
                foreach (var part in _bodySystem.GetBodyChildrenOfType(uid, BodyPartType.Hand))
                {
                    _transformSystem.AttachToGridOrMap(part.Id);
                }

                _popupSystem.PopupEntity(
                    Loc.GetString("admin-smite-remove-hands-self"),
                    uid,
                    uid,
                    PopupType.LargeCaution);

                _popupSystem.PopupCoordinates(
                    Loc.GetString("admin-smite-remove-hands-other", ("name", uid)),
                    baseXform.Coordinates,
                    Filter.PvsExcept(uid),
                    true,
                    PopupType.Medium);
            });
        }
    }
}
