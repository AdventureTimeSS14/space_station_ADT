//

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Heretic.Components;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed partial class MirrorMaidSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;

    public static readonly EntProtoId ExamineStatus = "StatusEffectStarMark";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MirrorMaidComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<MirrorMaidComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(Entity<MirrorMaidComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        foreach (var hit in args.HitEntities)
        {
            _status.TryAddStatusEffectDuration(hit, ExamineStatus, TimeSpan.FromSeconds(30));
        }
    }

    private void OnExamine(Entity<MirrorMaidComponent> ent, ref ExaminedEvent args)
    {
        if (args.Examiner == ent.Owner ||
            HasComp<GhostComponent>(args.Examiner) ||
            HasComp<SpectralComponent>(args.Examiner) ||
            _heretic.IsHereticOrGhoul(args.Examiner) ||
            HasComp<MirrorMaidComponent>(args.Examiner) ||
            _status.HasEffectComp<StarMarkStatusEffectComponent>(args.Examiner))
            return;

        var dmg = new DamageSpecifier
        {
            DamageDict =
            {
                { "Slash", ent.Comp.ExamineDamage },
            },
        };

        if (!_damageable.TryChangeDamage(ent.Owner, dmg, origin: args.Examiner))
            return;

        _status.TryAddStatusEffectDuration(args.Examiner, ExamineStatus, ent.Comp.ExamineDelay);

        _popup.PopupEntity(Loc.GetString("mirror-maid-examine-message-user",
                ("ent", Identity.Entity(ent, EntityManager, args.Examiner))),
            ent,
            args.Examiner);
        _popup.PopupEntity(Loc.GetString("mirror-maid-examine-message-maid",
                ("user", Identity.Entity(args.Examiner, EntityManager, ent))),
            ent,
            ent,
            PopupType.MediumCaution);
    }
}
