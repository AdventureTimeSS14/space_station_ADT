using Content.Server.Actions;
using Content.Server.ADT.Salvage.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Shared.Access.Systems;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Chasm;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Lathe;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class CursedHeartSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CursedHeartComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CursedHeartComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CursedHeartComponent, PumpHeartActionEvent>(OnPump);

        SubscribeLocalEvent<CursedHeartGrantComponent, UseInHandEvent>(OnUseInHand);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CursedHeartComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobState.IsDead(uid))
                continue;
            if (_timing.CurTime >= comp.LastPump + TimeSpan.FromSeconds(comp.MaxDelay))
            {
                Damage(uid);
                comp.LastPump = _timing.CurTime;
            }
        }
    }

    private void Damage(EntityUid uid)
    {
        _bloodstream.TryModifyBloodLevel(uid, -50, spill: false);
        _popup.PopupEntity(Loc.GetString("popup-cursed-heart-damage"), uid, uid, PopupType.MediumCaution);
    }

    private void OnMapInit(EntityUid uid, CursedHeartComponent comp, MapInitEvent args)
    {
        _actions.AddAction(uid, ref comp.PumpActionEntity, "ActionPumpCursedHeart");
    }

    private void OnShutdown(EntityUid uid, CursedHeartComponent comp, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, comp.PumpActionEntity);
    }

    private void OnPump(EntityUid uid, CursedHeartComponent comp, PumpHeartActionEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/ADT/Heretic/heartbeat.ogg"), uid);
        _damage.TryChangeDamage(uid, new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Brute"), -8), true, false);
        _bloodstream.TryModifyBloodLevel(uid, 17);
        comp.LastPump = _timing.CurTime;
    }

    private void OnUseInHand(EntityUid uid, CursedHeartGrantComponent comp, UseInHandEvent args)
    {
        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/ADT/Heretic/heartbeat.ogg"), args.User);
        var heart = EnsureComp<CursedHeartComponent>(args.User);
        heart.LastPump = _timing.CurTime;
        QueueDel(uid);
    }
}
