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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShadekinComponent, ShadekinTeleportActionEvent>(OnTeleport);
        SubscribeLocalEvent<ShadekinComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<ShadekinComponent, ComponentInit>(OnMapInit);
        SubscribeLocalEvent<ShadekinComponent, ComponentShutdown>(OnShutdown);
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
                comp.MinPowerAccumulator = 0f;

            if (comp.MinPowerAccumulator >= comp.MinPowerRoof)
                BlackEye(uid);
            if (comp.MaxedPowerAccumulator >= comp.MaxedPowerRoof)
                TeleportRandomly(uid);
        }
    }

    private void OnMapInit(EntityUid uid, ShadekinComponent comp, ComponentInit args)
    {
        _action.AddAction(uid, ref comp.ActionEntity, comp.ActionProto);
    }

    private void OnShutdown(EntityUid uid, ShadekinComponent comp, ComponentShutdown args)
    {
        _action.RemoveAction(uid, comp.ActionEntity);
    }

    private void OnTeleport(EntityUid uid, ShadekinComponent comp, ShadekinTeleportActionEvent args)
    {
        if (args.Handled)
            return;
        // if (_interaction.InRangeUnobstructed(uid, args.Target, -1f))
        //     return;
        if (!TryUseAbility(uid, 40))
            return;
        args.Handled = true;
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
            args.PushMarkup(Loc.GetString("shadekin-examine-self-" + level, ("power", comp.PowerLevel)));
        else
            args.PushMarkup(Loc.GetString("shadekin-examine-others-" + (comp.Blackeye ? "blackeye" : level)));
    }

    public void TeleportRandomly(EntityUid uid, ShadekinComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;
        var coordsValid = false;
        EntityCoordinates coords = Transform(uid).Coordinates;

        while (!coordsValid)
        {
            var newCoords = new EntityCoordinates(Transform(uid).ParentUid, coords.X + _random.NextFloat(-5f, 5f), coords.Y + _random.NextFloat(-5f, 5f));
            if (_interaction.InRangeUnobstructed(uid, newCoords, -1f))
            {
                TryUseAbility(uid, 40, false);
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

        _action.RemoveAction(comp.ActionEntity);
    }
}
