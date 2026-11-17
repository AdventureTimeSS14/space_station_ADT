using Content.Server.ADT.Virology;
using Content.Shared.ADT.Vampire;
using Content.Shared.ADT.Vampire.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Vampire;

public sealed partial class VampireSystem
{
    [Dependency] private ADTDiseaseSystem _disease = default!;
    [Dependency] private SharedJitteringSystem _jittering = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> WindowTag = "Window";
    private static readonly EntProtoId Stutter = "StatusEffectStutter";

    private void InitializeVariants()
    {
        SubscribeLocalEvent<VampireComponent, VampireGoonDiseaseActionEvent>(OnGoonDisease);
        SubscribeLocalEvent<VampireComponent, VampireBlinkActionEvent>(OnBlink);
        SubscribeLocalEvent<VampireComponent, VampireHypnotiseActionEvent>(OnHypnotise);
        SubscribeLocalEvent<VampireComponent, VampireHypnotiseDoAfterEvent>(OnHypnotiseDoAfter);
        SubscribeLocalEvent<VampireComponent, VampireScreechActionEvent>(OnScreech);
    }

    private void OnGoonDisease(Entity<VampireComponent> ent, ref VampireGoonDiseaseActionEvent args)
    {
        var target = args.Target;
        if (!HasComp<HumanoidProfileComponent>(target) || _mobState.IsDead(target)
            || HasComp<VampireComponent>(target) || HasComp<ThrallComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-disease-failed"), ent, ent, PopupType.SmallCaution);
            return;
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        if (_disease.Contract(target, args.Disease) == null)
        {
            _popup.PopupEntity(Loc.GetString("vampire-disease-failed"), ent, ent, PopupType.SmallCaution);
            return;
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        _popup.PopupEntity(Loc.GetString("vampire-disease-success"), ent, ent, PopupType.Small);
        args.Handled = true;
    }

    private void OnBlink(Entity<VampireComponent> ent, ref VampireBlinkActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var coords = Transform(ent).Coordinates;
        for (var i = 0; i < args.Attempts; i++)
        {
            var offset = _random.NextAngle().ToVec() * _random.NextFloat(args.InnerRadius, args.OuterRadius);
            var target = coords.Offset(offset);

            if (!_interaction.InRangeUnobstructed(ent, target, range: args.OuterRadius, collisionMask: CollisionGroup.Impassable, popup: false))
                continue;

            var mapCoords = _transform.ToMapCoordinates(target);
            if (!_map.TryFindGridAt(mapCoords, out var gridUid, out var grid))
                continue;

            if (!_map.TryGetTileRef(gridUid, grid, mapCoords.Position, out var tileRef))
                continue;

            if (FloorProto.Contains(_turf.GetContentTileDefinition(tileRef).ID))
                continue;

            _transform.SetCoordinates(ent, target);
            SubtractBloodEssence(ent.Owner, args.BloodCost);
            args.Handled = true;
            return;
        }

        _popup.PopupEntity(Loc.GetString("vampire-teleport-failed"), ent, ent, PopupType.Small);
    }

    private void OnHypnotise(Entity<VampireComponent> ent, ref VampireHypnotiseActionEvent args)
    {
        var target = args.Target;
        if (!HasComp<HumanoidProfileComponent>(target) || _mobState.IsDead(target)
            || HasComp<VampireComponent>(target) || HasComp<ThrallComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("vampire-hypnotise-failed"), ent, ent, PopupType.SmallCaution);
            return;
        }

        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        args.Handled = true;
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ent, args.Delay,
            new VampireHypnotiseDoAfterEvent(args.BloodCost, args.SleepTime), ent, target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1.5f,
            NeedHand = true
        });
    }

    private void OnHypnotiseDoAfter(Entity<VampireComponent> ent, ref VampireHypnotiseDoAfterEvent args)
    {
        if (args.Cancelled || args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;
        if (_mobState.IsDead(target) || HasComp<VampireComponent>(target))
            return;

        _status.TryAddStatusEffectDuration(target, ForceSleeping, args.SleepTime);
        SubtractBloodEssence(ent.Owner, args.BloodCost);
        _popup.PopupEntity(Loc.GetString("vampire-hypnotise-success"), ent, ent, PopupType.Small);
        _popup.PopupEntity(Loc.GetString("vampire-hypnotise-target"), target, target, PopupType.MediumCaution);
    }

    private void OnScreech(Entity<VampireComponent> ent, ref VampireScreechActionEvent args)
    {
        if (!CheckBloodEssence(ent.Owner, args.BloodCost))
        {
            SendFailedPopup(ent);
            return;
        }

        var coords = Transform(ent).Coordinates;
        _audio.PlayPvs(args.Sound, coords);

        foreach (var window in _entityLookup.GetEntitiesInRange<DamageableComponent>(coords, args.Range))
        {
            if (_tag.HasTag(window.Owner, WindowTag))
            {
                _damage.TryChangeDamage(window.Owner, new DamageSpecifier
                {
                    DamageDict = { { "Structural", 60 } }
                }, origin: ent);
            }
        }

        foreach (var victim in _entityLookup.GetEntitiesInRange<MobStateComponent>(coords, args.Range))
        {
            if (victim.Owner == ent.Owner || _mobState.IsDead(victim.Owner)
                || HasComp<VampireComponent>(victim.Owner) || HasComp<ThrallComponent>(victim.Owner))
                continue;

            _stun.TryKnockdown(victim.Owner, args.KnockdownTime);
            _jittering.AddJitter(victim.Owner, 80f, 8f);
            Timer.Spawn(args.JitterTime, () => RemCompDeferred<JitteringComponent>(victim.Owner));
            _status.TryAddStatusEffectDuration(victim.Owner, Stutter, args.StutterTime);
            _stamina.TakeStaminaDamage(victim.Owner, args.StaminaDamage);
        }

        SubtractBloodEssence(ent.Owner, args.BloodCost);
        args.Handled = true;
    }
}