using Content.Server.Body.Components;
using Content.Server.Chat.Systems;
using Content.Server.Temperature.Systems;
using Content.Shared.ADT.AshWalker;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.ADT.Lavaland.LegionCore;
using Content.Shared.Chat;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Temperature.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTLavalandFoodSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IngestionSystem _ingestion = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTPeriodicEffectsStatusEffectComponent, StatusEffectAppliedEvent>(OnPeriodicApplied);
        SubscribeLocalEvent<ADTPeriodicEffectsStatusEffectComponent, StatusEffectRemovedEvent>(OnPeriodicRemoved);
        SubscribeLocalEvent<ADTTemperatureStabilizeStatusEffectComponent, StatusEffectAppliedEvent>(OnStabilizeApplied);
        SubscribeLocalEvent<ADTTemperatureStabilizeStatusEffectComponent, StatusEffectRemovedEvent>(OnStabilizeRemoved);
        SubscribeLocalEvent<ADTStrongMusclesComponent, GetMeleeDamageEvent>(OnStrongMeleeDamage);
        SubscribeLocalEvent<ADTCureCurseComponent, ADTHealTouchUsedEvent>(OnCureCurseTouched);
        SubscribeLocalEvent<ADTCureCurseComponent, ExaminedEvent>(OnCureCurseExamined);
        SubscribeLocalEvent<ADTCureCurseComponent, IngestedEvent>(OnCureCurseIngested);
        SubscribeLocalEvent<ADTEatOnThrowHitComponent, ThrowDoHitEvent>(OnThrowHit);
        SubscribeLocalEvent<ADTStatusEffectsOnIngestComponent, IngestedEvent>(OnStatusEffectsIngested);
    }

    private void OnPeriodicApplied(Entity<ADTPeriodicEffectsStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.Target = args.Target;
        ent.Comp.NextTick = _timing.CurTime + ent.Comp.Interval;
    }

    private void OnPeriodicRemoved(Entity<ADTPeriodicEffectsStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        ent.Comp.Target = null;
    }

    private void OnStabilizeApplied(Entity<ADTTemperatureStabilizeStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.Target = args.Target;
        ent.Comp.NextTick = _timing.CurTime + ent.Comp.Interval;
    }

    private void OnStabilizeRemoved(Entity<ADTTemperatureStabilizeStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        ent.Comp.Target = null;
    }

    private void OnStrongMeleeDamage(Entity<ADTStrongMusclesComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (args.Weapon != ent.Owner)
            return;

        args.Damage *= ent.Comp.UnarmedDamageMultiplier;
    }

    private void OnCureCurseTouched(Entity<ADTCureCurseComponent> ent, ref ADTHealTouchUsedEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Active)
        {
            _popup.PopupEntity(Loc.GetString("adt-cure-curse-already-active"), ent.Owner, args.User);
            return;
        }

        ent.Comp.Active = true;
        _appearance.SetData(ent.Owner, ADTCureCurseVisuals.Active, true);
        _chat.TrySendInGameICMessage(args.User, Loc.GetString(ent.Comp.ActivateSpeech), InGameICChatType.Speak, false);
    }

    private void OnCureCurseExamined(Entity<ADTCureCurseComponent> ent, ref ExaminedEvent args)
    {
        var message = ent.Comp.Active
            ? "adt-cure-curse-examine-active"
            : "adt-cure-curse-examine-inactive";

        args.PushMarkup(Loc.GetString(message));
    }

    private void OnCureCurseIngested(Entity<ADTCureCurseComponent> ent, ref IngestedEvent args)
    {
        if (!ent.Comp.Active || HasComp<ADTImplantedLegionCoreComponent>(args.Target))
            return;

        EnsureComp<ADTImplantedLegionCoreComponent>(args.Target);
        _popup.PopupEntity(Loc.GetString("adt-cure-curse-implanted"), args.Target, args.Target);
    }

    private void OnStatusEffectsIngested(Entity<ADTStatusEffectsOnIngestComponent> ent, ref IngestedEvent args)
    {
        foreach (var (effect, duration) in ent.Comp.Effects)
        {
            _status.TryUpdateStatusEffectDuration(args.Target, effect, duration);
        }
    }

    private void OnThrowHit(Entity<ADTEatOnThrowHitComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!HasComp<HumanoidProfileComponent>(args.Target))
            return;

        _ingestion.TryIngest(args.Target, ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;

        var periodic = EntityQueryEnumerator<ADTPeriodicEffectsStatusEffectComponent>();
        while (periodic.MoveNext(out _, out var comp))
        {
            if (comp.Target is not { } target || TerminatingOrDeleted(target))
                continue;

            if (comp.NextTick > curTime)
                continue;

            comp.NextTick = curTime + comp.Interval;
            _effects.ApplyEffects(target, comp.Effects);
        }

        var stabilize = EntityQueryEnumerator<ADTTemperatureStabilizeStatusEffectComponent>();
        while (stabilize.MoveNext(out _, out var comp))
        {
            if (comp.Target is not { } target || TerminatingOrDeleted(target))
                continue;

            if (comp.NextTick > curTime)
                continue;

            comp.NextTick = curTime + comp.Interval;
            Stabilize(target, comp.Step);
        }
    }

    private void Stabilize(EntityUid target, float step)
    {
        if (!TryComp<TemperatureComponent>(target, out var temperature) || !TryComp<ThermalRegulatorComponent>(target, out var regulator))
            return;

        var difference = temperature.CurrentTemperature - regulator.NormalBodyTemperature;
        if (MathF.Abs(difference) <= step)
            return;

        var change = difference > 0 ? -step : step;
        _temperature.ForceChangeTemperature(target, temperature.CurrentTemperature + change, temperature);
    }
}
