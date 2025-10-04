using Content.Shared.Revenant;
using Content.Shared.Revenant.Components;
using Robust.Shared.Audio;
using Content.Server.GameTicking;

namespace Content.Server.Revenant.EntitySystems;

public sealed partial class RevenantSystem  // ADT File
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    private void InitializeInstantEffects()
    {
        SubscribeLocalEvent<RevenantComponent, AddRevenantShieldEvent>(OnBuyShield);
        SubscribeLocalEvent<RevenantComponent, StartRevenantMiseryEvent>(OnBuyMisery);

        SubscribeLocalEvent<RevenantMiseryComponent, MapInitEvent>(OnMiseryInit);
        SubscribeLocalEvent<RevenantMiseryComponent, ComponentShutdown>(OnMiseryShutdown);
    }

    private void OnBuyShield(EntityUid uid, RevenantComponent comp, AddRevenantShieldEvent args)
    {
        EnsureComp<RevenantShieldComponent>(uid);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/injury.ogg"), Transform(uid).Coordinates);
        _stun.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(2.5f));
    }

    private void OnBuyMisery(EntityUid uid, RevenantComponent comp, StartRevenantMiseryEvent args)
    {
        EnsureComp<RevenantMiseryComponent>(uid);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/puppeter.ogg"), Transform(uid).Coordinates);
        _stun.TryUpdateParalyzeDuration(uid, TimeSpan.FromSeconds(4.5f));
    }

    private void OnMiseryInit(EntityUid uid, RevenantMiseryComponent comp, MapInitEvent args)
    {
        var rule = _gameTicker.AddGameRule("RampingStationEventScheduler");
        _gameTicker.StartGameRule(rule);
        comp.Event = rule;
    }

    private void OnMiseryShutdown(EntityUid uid, RevenantMiseryComponent comp, ComponentShutdown args)
    {
        _gameTicker.EndGameRule(comp.Event);
    }
}
