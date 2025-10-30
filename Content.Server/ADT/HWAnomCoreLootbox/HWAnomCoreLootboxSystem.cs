using System.Reflection.Metadata;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Part;
using Content.Shared.Bible.Components;
using Content.Shared.Cargo;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
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
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HWAnomCoreLootboxComponent, UseInHandEvent>(OnUseInHand);
        }
        private void OnUseInHand(EntityUid uid, HWAnomCoreLootboxComponent component, UseInHandEvent args)
        {
            float choosenumber = _random.NextFloat(1f, 10f);
            switch (choosenumber)
            {
                case > 0f and < 4f:
                    ChaplainDo(args.User);
                    break;
                case > 3f and < 6f:
                    Blind(component, args.User);
                    break;
                case > 6f and < 8f:
                    Hemophilia(args.User);
                    break;
                case > 8f and < 10f:
                    Paracusia(args.User);
                    break;
                case 10f:
                    Damageplayer(args.User);
                    break;

            }
        }
        private void ChaplainDo(EntityUid uid)
        {
            if (!HasComp<ChaplainComponent>(uid))
            {
                EnsureComp<ChaplainComponent>(uid);
            }
        }

        private void Blind(HWAnomCoreLootboxComponent comp, EntityUid user)
        {
            if (!HasComp<HWAnomCoreLootboxComponent>(comp.Owner))
                return;
            _status.TryAddStatusEffect<TemporaryBlindnessComponent>(user, TemporaryBlindnessSystem.BlindingStatusEffect, TimeSpan.FromSeconds(comp.Duration), true);
        }

        private void Hemophilia(EntityUid uid)
        {
            if (!HasComp<HemophiliaComponent>(uid))
            {
                EnsureComp<HemophiliaComponent>(uid);
            }
        }

        private void Paracusia(EntityUid uid)
        {
            if (!HasComp<ParacusiaComponent>(uid))
            {
                EnsureComp<ParacusiaComponent>(uid);
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
