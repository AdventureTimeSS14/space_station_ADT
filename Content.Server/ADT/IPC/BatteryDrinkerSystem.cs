using System.Diagnostics.CodeAnalysis;
using Content.Shared.ADT.IPC;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Server.ADT.IPC;

public sealed class BatteryDrinkerSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SiliconSystem _silicon = default!;

    private const float SipFraction = 0.1f;
    private const float FullFraction = 0.95f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryComponent, GetVerbsEvent<AlternativeVerb>>(AddAltVerb);
        SubscribeLocalEvent<BatteryDrinkerComponent, BatteryDrinkerDoAfterEvent>(OnDoAfter);
    }

    private void AddAltVerb(Entity<BatteryComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!TryComp<BatteryDrinkerComponent>(args.User, out var drinker)
            || (!drinker.DrinkAll && !HasComp<BatteryDrinkerSourceComponent>(ent))
            || !TryGetFillableBattery(args.User, out _))
            return;

        var user = args.User;
        var target = ent.Owner;
        args.Verbs.Add(new AlternativeVerb
        {
            Act = () => StartDrinking(target, user, drinker),
            Text = Loc.GetString("battery-drinker-verb-drink"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/smite.svg.192dpi.png")),
        });
    }

    private bool TryGetFillableBattery(EntityUid uid, [NotNullWhen(true)] out Entity<BatteryComponent>? battery)
    {
        if (_silicon.TryGetSiliconBattery(uid, out battery))
            return true;

        if (TryComp<BatteryComponent>(uid, out var own))
        {
            battery = (uid, own);
            return true;
        }

        battery = null;
        return false;
    }

    private void StartDrinking(EntityUid target, EntityUid user, BatteryDrinkerComponent drinker)
    {
        var delay = drinker.DrinkSpeed;
        if (TryComp<BatteryDrinkerSourceComponent>(target, out var source))
            delay *= source.DrinkSpeedMulti;
        else
            delay *= drinker.DrinkAllMultiplier;

        var args = new DoAfterArgs(EntityManager, user, delay, new BatteryDrinkerDoAfterEvent(), user, target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 1.35f,
            RequireCanInteract = true,
            CancelDuplicate = false,
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void OnDoAfter(Entity<BatteryDrinkerComponent> ent, ref BatteryDrinkerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!TryComp<BatteryComponent>(target, out var sourceBattery)
            || !TryGetFillableBattery(ent, out var drinkerBattery))
            return;

        TryComp<BatteryDrinkerSourceComponent>(target, out var source);
        if (source == null && !ent.Comp.DrinkAll)
            return;

        args.Handled = true;

        var maxCharge = drinkerBattery.Value.Comp.MaxCharge;
        var amount = MathF.Min(maxCharge * SipFraction, _battery.GetCharge((target, sourceBattery)));
        amount = MathF.Min(amount, maxCharge - _battery.GetCharge(drinkerBattery.Value.AsNullable()));

        if (source?.MaxAmount > 0)
            amount = MathF.Min(amount, source.MaxAmount.Value);

        if (!float.IsFinite(amount) || amount <= 0)
        {
            _popup.PopupEntity(Loc.GetString("battery-drinker-empty", ("target", target)), ent, ent);
            return;
        }

        if (!_battery.TryUseCharge((target, sourceBattery), amount))
            return;

        _battery.ChangeCharge(drinkerBattery.Value.AsNullable(), amount);
        _audio.PlayPvs(source?.DrinkSound, target);

        args.Repeat = _battery.GetCharge(drinkerBattery.Value.AsNullable()) < maxCharge * FullFraction;
    }
}
