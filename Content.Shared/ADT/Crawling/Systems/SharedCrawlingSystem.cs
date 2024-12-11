using Content.Shared.DoAfter;
using Content.Shared.Explosion;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Content.Shared.Standing;
using Robust.Shared.Serialization;
using Content.Shared.Stunnable;
using Robust.Shared.Player;
using Content.Shared.Movement.Systems;
using Content.Shared.Alert;
using Content.Shared.Climbing.Components;
using Content.Shared.Popups;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Climbing.Events;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.ADT.Crawling;

public abstract class SharedCrawlingSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrawlerComponent, CrawlStandupDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<CrawlerComponent, StandAttemptEvent>(OnStandUp);
        SubscribeLocalEvent<CrawlerComponent, DownAttemptEvent>(OnFall);
        SubscribeLocalEvent<CrawlerComponent, StunnedEvent>(OnStunned);
        SubscribeLocalEvent<CrawlerComponent, GetExplosionResistanceEvent>(OnGetExplosionResistance);
        SubscribeLocalEvent<CrawlerComponent, CrawlingKeybindEvent>(ToggleCrawling);

        SubscribeLocalEvent<CrawlingComponent, ComponentInit>(OnCrawlSlowdownInit);
        SubscribeLocalEvent<CrawlingComponent, ComponentShutdown>(OnCrawlSlowRemove);
        SubscribeLocalEvent<CrawlingComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);

        SubscribeLocalEvent<CrawlerComponent, MapInitEvent>(OnCrawlerInit);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleCrawling, InputCmdHandler.FromDelegate(ToggleCrawlingKeybind, handle: false))
            .Register<SharedCrawlingSystem>();
    }

    private void ToggleCrawlingKeybind(ICommonSession? session)
    {
        if (session?.AttachedEntity == null)
            return;
        var ev = new CrawlingKeybindEvent();
        RaiseLocalEvent(session.AttachedEntity.Value, ev);
    }
    private void ToggleCrawling(EntityUid uid, CrawlerComponent component, CrawlingKeybindEvent args)
    {
        ///checks players standing state, downing player if they are standding and starts doafter with standing up if they are downed
        switch (_standing.IsDown(uid))
        {
            case false:
                _standing.Down(uid, dropHeldItems: false);
                break;
            case true:
                _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.StandUpTime, new CrawlStandupDoAfterEvent(),
                uid, used: uid)
                {
                    BreakOnDamage = true
                });
                break;
        }
    }
    private void OnDoAfter(EntityUid uid, CrawlerComponent component, CrawlStandupDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var item in _lookup.GetEntitiesInRange<ClimbableComponent>(Transform(uid).Coordinates, 0.25f))
        {
            if (HasComp<ClimbableComponent>(item))
            {
                TableVictim(uid, component);
                return;
            }
        }
        _standing.Stand(uid);
    }

    private void TableVictim(EntityUid uid, CrawlerComponent component)
    {
        var stunTime = component.DefaultStunTime;

        _stun.TryParalyze(uid, stunTime, true);
        _audio.PlayPvs(component.TableBonkSound, uid);
    }

    private void OnStandUp(EntityUid uid, CrawlerComponent component, StandAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        RemCompDeferred<CrawlingComponent>(uid);
        _alerts.ClearAlert(uid, component.CtawlingAlert);
    }
    private void OnFall(EntityUid uid, CrawlerComponent component, DownAttemptEvent args)
    {
        if (args.Cancelled)
            return;
        _alerts.ShowAlert(uid, component.CtawlingAlert);
        if (!HasComp<CrawlingComponent>(uid))
            AddComp<CrawlingComponent>(uid);
        //TODO: add hiding under table
    }
    private void OnStunned(EntityUid uid, CrawlerComponent component, StunnedEvent args)
    {
        if (!HasComp<CrawlingComponent>(uid))
            AddComp<CrawlingComponent>(uid);
        _alerts.ShowAlert(uid, component.CtawlingAlert);
    }
    private void OnGetExplosionResistance(EntityUid uid, CrawlerComponent component, ref GetExplosionResistanceEvent args)
    {
        // fall on explosion damage and lower explosion damage of crawling
        if (_standing.IsDown(uid))
            args.DamageCoefficient *= component.DownedDamageCoefficient;
        else
        {
            var ev = new ExplosionDownAttemptEvent(args.ExplosionPrototype);
            RaiseLocalEvent(uid, ref ev);
            if (!ev.Cancelled)
                _standing.Down(uid);
        }

    }
    private void OnCrawlSlowdownInit(EntityUid uid, CrawlingComponent component, ComponentInit args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        _appearance.SetData(uid, CrawlingVisuals.Standing, false);
        _appearance.SetData(uid, CrawlingVisuals.Crawling, true);
    }
    private void OnCrawlSlowRemove(EntityUid uid, CrawlingComponent component, ComponentShutdown args)
    {
        component.SprintSpeedModifier = 1f;
        component.WalkSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        _appearance.SetData(uid, CrawlingVisuals.Crawling, false);
        _appearance.SetData(uid, CrawlingVisuals.Standing, true);
    }
    private void OnRefreshMovespeed(EntityUid uid, CrawlingComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    private void OnCrawlerInit(EntityUid uid, CrawlerComponent comp, MapInitEvent args)
    {
        _appearance.SetData(uid, CrawlingVisuals.Standing, true);
        _appearance.SetData(uid, CrawlingVisuals.Crawling, false);
    }
}

[Serializable, NetSerializable]
public sealed partial class CrawlStandupDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class CrawlingKeybindEvent
{
}

[NetSerializable, Serializable]
public enum CrawlingVisuals : byte
{
    Standing,
    Crawling,
}
