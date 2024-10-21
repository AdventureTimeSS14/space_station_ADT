using Content.Shared.ADT.Shadekin.Components;
using Robust.Shared.Timing;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Server.Humanoid;
using Content.Shared.ADT.Shadekin;
using System.Numerics;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Content.Shared.Effects;
using Robust.Shared.Player;
using Robust.Server.Audio;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Shared.Examine;
using Content.Server.Actions;
using Content.Server.Station.Systems;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Mobs.Systems;
using Content.Server.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Mech.Components;
using Content.Server.Disposal.Unit.Components;

namespace Content.Server.ADT.Shadekin;

public sealed partial class ShadekinSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming _timing = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly AlertsSystem _alert = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly CuffableSystem _cuffable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadekinComponent, ShadekinTeleportActionEvent>(OnTeleport);
        SubscribeLocalEvent<ShadekinComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ShadekinComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShadekinComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ShadekinComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ShadekinComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextSecond > _timing.CurTime)
                continue;
            if (comp.Blackeye)
                continue;
            if (_mobState.IsIncapacitated(uid))
                continue;

            _alert.ShowAlert(uid, _proto.Index<AlertPrototype>("ShadekinPower"), (short) Math.Clamp(Math.Round(comp.PowerLevel / 50f), 0, 4));
            comp.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);

            if (comp.PowerLevel >= comp.PowerLevelMax)
                comp.MaxedPowerAccumulator += 1f;
            else
            {
                comp.PowerLevel += comp.PowerLevelGain * comp.PowerLevelGainMultiplier;
                comp.MaxedPowerAccumulator = 0f;
            }

            if (comp.PowerLevel < comp.PowerLevelMin)
                comp.MinPowerAccumulator += 1f;
            else
                comp.MinPowerAccumulator = Math.Clamp(comp.MinPowerAccumulator - 1f, 0f, comp.MinPowerRoof);

            if (comp.MinPowerAccumulator >= comp.MinPowerRoof)
                BlackEye(uid);
            if (comp.MaxedPowerAccumulator >= comp.MaxedPowerRoof)
                TeleportRandomly(uid, comp);
        }
    }

    private void OnInit(EntityUid uid, ShadekinComponent comp, ComponentInit args)
    {
        _alert.ShowAlert(uid, _proto.Index<AlertPrototype>("ShadekinPower"), (short) Math.Clamp(Math.Round(comp.PowerLevel / 50f), 0, 4));
    }

    private void OnShutdown(EntityUid uid, ShadekinComponent comp, ComponentShutdown args)
    {
        _alert.ClearAlert(uid, _proto.Index<AlertPrototype>("ShadekinPower"));
        if (comp.ActionEntity != null)
            _action.RemoveAction(uid, comp.ActionEntity);
    }

    private void OnMapInit(EntityUid uid, ShadekinComponent comp, MapInitEvent args)
    {
        // if ( // Оно очень странно получается, работает только при позднем подключении
        //     args.Profile.Appearance.EyeColor.B < 100f &&
        //     args.Profile.Appearance.EyeColor.R < 100f &&
        //     args.Profile.Appearance.EyeColor.G < 100f)
        // {
        //     comp.Blackeye = true;
        //     comp.PowerLevelGainEnabled = false;
        //     comp.PowerLevel = 0f;
        //     return;
        // }
        _alert.ShowAlert(uid, _proto.Index<AlertPrototype>("ShadekinPower"), (short) Math.Clamp(Math.Round(comp.PowerLevel / 50f), 0, 4));
        _action.AddAction(uid, ref comp.ActionEntity, comp.ActionProto);
    }

    private void OnTeleport(EntityUid uid, ShadekinComponent comp, ShadekinTeleportActionEvent args)
    {
        if (args.Handled)
            return;
        // if (_interaction.InRangeUnobstructed(uid, args.Target, -1f))
        //     return;

        if (
            HasComp<MechPilotComponent>(uid)
            || HasComp<BeingDisposedComponent>(uid)
        )
        {
            return;
        }

        if (!TryUseAbility(uid, 50))
            return;
        args.Handled = true;

        if (TryComp<PullerComponent>(uid, out var puller) && puller.Pulling != null && TryComp<PullableComponent>(puller.Pulling, out var pullable))
            _pulling.TryStopPull(puller.Pulling.Value, pullable);
        _transform.SetCoordinates(uid, args.Target);
        _colorFlash.RaiseEffect(Color.DarkCyan, new List<EntityUid>() { uid }, Filter.Pvs(uid, entityManager: EntityManager));
        _audio.PlayPvs("/Audio/ADT/Shadekin/shadekin-transition.ogg", uid);
    }

    private void OnExamine(EntityUid uid, ShadekinComponent comp, ExaminedEvent args)
    {
        var level = "max";
        if (comp.PowerLevel == 250f)
            level = "max";
        if (comp.PowerLevel < 250f)
            level = "good";
        if (comp.PowerLevel <= 200f)
            level = "okay";
        if (comp.PowerLevel <= 100f)
            level = "bad";
        if (comp.PowerLevel <= 50f)
            level = "worst";

        if (args.Examiner == uid)
            args.PushMarkup(Loc.GetString("shadekin-examine-self-" + level, ("power", comp.PowerLevel.ToString())));
    }

    public void TeleportRandomly(EntityUid uid, ShadekinComponent? comp)
    {
        if (!Resolve(uid, ref comp))
            return;
        var coordsValid = false;
        EntityCoordinates coords = Transform(uid).Coordinates;

        if (
            (TryComp<CuffableComponent>(uid, out var cuffable) && _cuffable.IsCuffed((uid, cuffable), true))
            || HasComp<MechPilotComponent>(uid)
            || HasComp<BeingDisposedComponent>(uid)
        )
        {
            comp.MaxedPowerAccumulator = 0f;
            return;
        }

        if (TryComp<PullableComponent>(uid, out var mainEntityPullable) && _pulling.IsPulled(uid, mainEntityPullable)) {
            _pulling.TryStopPull(uid, mainEntityPullable);
        }

        while (!coordsValid)
        {
            var newCoords = new EntityCoordinates(Transform(uid).ParentUid, coords.X + _random.NextFloat(-5f, 5f), coords.Y + _random.NextFloat(-5f, 5f));
            if (_interaction.InRangeUnobstructed(uid, newCoords, -1f))
            {
                TryUseAbility(uid, 40, false);
                if (TryComp<PullerComponent>(uid, out var puller) && puller.Pulling != null && TryComp<PullableComponent>(puller.Pulling, out var pullable))
                    _pulling.TryStopPull(puller.Pulling.Value, pullable);
                _alert.ShowAlert(uid, _proto.Index<AlertPrototype>("ShadekinPower"), (short) Math.Clamp(Math.Round(comp.PowerLevel / 50f), 0, 4));
                _transform.SetCoordinates(uid, newCoords);
                _transform.AttachToGridOrMap(uid, Transform(uid));
                _colorFlash.RaiseEffect(Color.DarkCyan, new List<EntityUid>() { uid }, Filter.Pvs(uid, entityManager: EntityManager));
                _audio.PlayPvs("/Audio/ADT/Shadekin/shadekin-transition.ogg", uid);
                comp.MaxedPowerAccumulator = 0f;
                coordsValid = true;
                break;
            }
        }
    }

    public void TeleportRandomlyNoComp(EntityUid uid, float range = 5f)
    {
        var coordsValid = false;
        EntityCoordinates coords = Transform(uid).Coordinates;

        while (!coordsValid)
        {
            var newCoords = new EntityCoordinates(Transform(uid).ParentUid, coords.X + _random.NextFloat(-range, range), coords.Y + _random.NextFloat(-range, range));
            if (_interaction.InRangeUnobstructed(uid, newCoords, -1f))
            {
                if (TryComp<PullerComponent>(uid, out var puller) && puller.Pulling != null && TryComp<PullableComponent>(puller.Pulling, out var pullable))
                    _pulling.TryStopPull(puller.Pulling.Value, pullable);
                _transform.SetCoordinates(uid, newCoords);
                _transform.AttachToGridOrMap(uid, Transform(uid));
                _colorFlash.RaiseEffect(Color.DarkCyan, new List<EntityUid>() { uid }, Filter.Pvs(uid, entityManager: EntityManager));
                _audio.PlayPvs("/Audio/ADT/Shadekin/shadekin-transition.ogg", uid);
                coordsValid = true;
                break;
            }
        }
    }

    public bool TryUseAbility(EntityUid uid, FixedPoint2 cost, bool allowBlackeye = true, ShadekinComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;
        if (comp.PowerLevel <= cost && allowBlackeye)
        {
            BlackEye(uid);
            return false;
        }
        comp.PowerLevel -= cost.Float();
        _alert.ShowAlert(uid, _proto.Index<AlertPrototype>("ShadekinPower"), (short) Math.Clamp(Math.Round(comp.PowerLevel / 50f), 0, 4));
        return true;
    }

    public void BlackEye(EntityUid uid, ShadekinComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        comp.Blackeye = true;
        comp.PowerLevelGainEnabled = false;
        comp.PowerLevel = 0f;
        _stamina.TakeStaminaDamage(uid, 150f);
        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            humanoid.EyeColor = Color.Black;
            Dirty(uid, humanoid);
        }
        _alert.ShowAlert(uid, _proto.Index<AlertPrototype>("ShadekinPower"), (short) Math.Clamp(Math.Round(comp.PowerLevel / 50f), 0, 5));
        _action.RemoveAction(comp.ActionEntity);
    }
}
