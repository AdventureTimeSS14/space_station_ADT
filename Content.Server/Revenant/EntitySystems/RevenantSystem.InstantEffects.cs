using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.Revenant;
using Robust.Shared.Random;
using Content.Shared.Tag;
using Content.Server.Storage.Components;
using Content.Server.Light.Components;
using Content.Server.Ghost;
using Robust.Shared.Physics;
using Content.Shared.Throwing;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Bed.Sleep;
using System.Linq;
using System.Numerics;
using Content.Server.Revenant.Components;
using Content.Shared.Physics;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Revenant.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;
using Content.Server.ADT.Hallucinations;
using Content.Shared.StatusEffect;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Server.Fluids.EntitySystems;
using Robust.Shared.Player;
using Robust.Shared.Audio.Systems;
using Content.Shared.Mind;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Tools.Systems;
using Content.Shared.Chemistry.Components;
using Robust.Shared.Map.Components;
using Content.Shared.Whitelist;
using Content.Shared.ADT.Silicon.Components;
using Content.Shared.Stunnable;
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
        _stun.TryParalyze(uid, TimeSpan.FromSeconds(2.5f), true);
    }

    private void OnBuyMisery(EntityUid uid, RevenantComponent comp, StartRevenantMiseryEvent args)
    {
        EnsureComp<RevenantMiseryComponent>(uid);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/puppeter.ogg"), Transform(uid).Coordinates);
        _stun.TryParalyze(uid, TimeSpan.FromSeconds(4.5f), true);
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
