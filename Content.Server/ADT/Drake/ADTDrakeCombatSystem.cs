using Content.Server.ADT.Salvage.Systems;
using Content.Shared.ADT.Drake;
using Content.Shared.ADT.Salvage.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.ADT.Drake;

public sealed class ADTDrakeCombatSystem : EntitySystem
{
    [Dependency] private ADTDrakeAttacksSystem _attacks = default!;
    [Dependency] private ADTDrakeSystem _drake = default!;
    [Dependency] private ADTDrakeSwoopSystem _swoop = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDrakeComponent, ADTDrakeMeleeHitLivingEvent>(OnMeleeHitLiving);
        SubscribeLocalEvent<ADTDrakeComponent, ADTMegafaunaAttemptOpenFireEvent>(OnAttemptOpenFire);
        SubscribeLocalEvent<ADTDrakeComponent, ADTMegafaunaOpenFireEvent>(OnOpenFire);
    }

    private void OnAttemptOpenFire(Entity<ADTDrakeComponent> ent, ref ADTMegafaunaAttemptOpenFireEvent args)
    {
        if (_swoop.IsSwooping(ent))
            args.Cancelled = true;
    }

    private void OnOpenFire(Entity<ADTDrakeComponent> ent, ref ADTMegafaunaOpenFireEvent args)
    {
        OpenFire(ent, args.Target);
    }

    private void OnMeleeHitLiving(Entity<ADTDrakeComponent> ent, ref ADTDrakeMeleeHitLivingEvent args)
    {
        if (HasComp<ActorComponent>(ent))
            return;

        if (!TryComp<ADTMegafaunaAIComponent>(ent, out var ai) || !_megafauna.IsRangedReady((ent.Owner, ai)))
            return;

        if (_swoop.IsSwooping(ent))
            return;

        OpenFire(ent, args.Target);
    }

    public void OpenFire(Entity<ADTDrakeComponent> ent, EntityUid target)
    {
        _drake.Aggro(ent, target);

        var anger = _drake.CalculateAnger(ent);

        if (TryComp<ADTMegafaunaAIComponent>(ent, out var ai))
            _megafauna.SetRangedCooldown((ent.Owner, ai), ent.Comp.RangedCooldown);

        if (_random.Prob(Math.Clamp(ent.Comp.LavaSwoopChanceBase + anger / 100f, 0f, 1f)))
        {
            _attacks.LavaSwoop(ent, target);
            return;
        }

        if (_random.Prob(Math.Clamp(ent.Comp.ShootFireChanceBase + anger / 100f, 0f, 1f)))
        {
            _attacks.ShootFireAttack(ent, target);
            return;
        }

        _attacks.FireCone(ent, target);
    }
}

[ByRefEvent]
public readonly record struct ADTDrakeMeleeHitLivingEvent(EntityUid Target);
