//

using Content.Shared.ADT.Heretic.Common;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Electrocution;
using Content.Shared.Explosion;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Content.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Shared.Popups;
using Content.Shared.Slippery;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Blade;

public abstract partial class SharedBladeArenaSystem : EntitySystem
{
    public static readonly EntProtoId StatusEffectStunned = "StatusEffectStunned";

    [Dependency] private readonly SharedPopupSystem _popup = default!;

    [Dependency] private readonly EntityQuery<InsideArenaComponent> _insideQuery = default!;
    [Dependency] protected readonly EntityQuery<HereticArenaParticipantComponent> ParticipantQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HereticArenaParticipantComponent, ElectrocutionAttemptEvent>(OnElectrocuteAttempt);
        SubscribeLocalEvent<HereticArenaParticipantComponent, BeforeStatusEffectAddedEvent>(OnBeforeStatusEffect);
        SubscribeLocalEvent<HereticArenaParticipantComponent, BeforeStaminaDamageEvent>(OnBeforeStaminaDamage);
        SubscribeLocalEvent<HereticArenaParticipantComponent, GetExplosionResistanceEvent>(OnGetExplosionResists);
        SubscribeLocalEvent<HereticArenaParticipantComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<HereticArenaParticipantComponent, SlipAttemptEvent>(OnSlipAttempt);
        SubscribeLocalEvent<HereticArenaOuterWallComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<HereticArenaParticipantComponent, TeleportAttemptEvent>(OnTeleportAttempt);
    }

    private void OnElectrocuteAttempt(Entity<HereticArenaParticipantComponent> ent, ref ElectrocutionAttemptEvent args)
    {
        if (IsInsideArena(ent))
            args.Cancel();
    }

    private void OnBeforeStatusEffect(Entity<HereticArenaParticipantComponent> ent, ref BeforeStatusEffectAddedEvent args)
    {
        if (args.Effect == StatusEffectStunned)
            args.Cancelled |= IsInsideArena(ent);
    }

    private void OnBeforeStaminaDamage(Entity<HereticArenaParticipantComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        args.Cancelled |= IsInsideArena(ent);
    }

    private void OnGetExplosionResists(Entity<HereticArenaParticipantComponent> ent, ref GetExplosionResistanceEvent args)
    {
        if (!IsInsideArena(ent))
            return;

        args.DamageCoefficient = 0f;
    }

    private void OnDamageModify(Entity<HereticArenaParticipantComponent> ent, ref DamageModifyEvent args)
    {
        if (!IsInsideArena(ent))
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, ent.Comp.ModifierSet);
    }

    private void OnSlipAttempt(Entity<HereticArenaParticipantComponent> ent, ref SlipAttemptEvent args)
    {
        args.NoSlip |= IsInsideArena(ent);
    }

    private void OnPreventCollide(Entity<HereticArenaOuterWallComponent> ent, ref PreventCollideEvent args)
    {
        var other = args.OtherEntity;
        args.Cancelled = ParticipantQuery.TryComp(other, out var participant) && participant.IsVictor ||
                         HasComp<GhoulComponent>(other);
    }

    private void OnTeleportAttempt(Entity<HereticArenaParticipantComponent> ent, ref TeleportAttemptEvent args)
    {
        if (ent.Comp.IsVictor)
            return;

        args.Cancelled = true;

        if (args.Message == null)
            return;

        var msg = Loc.GetString(args.Message);
        _popup.PopupEntity(msg, ent, ent);
    }

    protected bool IsInsideArena(EntityUid uid)
        => _insideQuery.HasComp(uid);
}
