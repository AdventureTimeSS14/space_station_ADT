using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Effects;
using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using System.Numerics;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;

namespace Content.Server.ADT.Virology;

public sealed class ADTSymptomAggressionSystem : EntitySystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly HashSet<Entity<MobStateComponent>> _targets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ADTSymptomEvent<ADTSymptomAggression>>(OnAggression);
    }

    private void OnAggression(Entity<ADTDiseaseCarrierComponent> ent, ref ADTSymptomEvent<ADTSymptomAggression> args)
    {
        if (_mobState.IsIncapacitated(ent) || _standing.IsDown(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("adt-symptom-aggression-twitch", ("name", Name(ent))), ent, PopupType.MediumCaution);
            return;
        }

        if (!_random.Prob(args.Effect.AttackChance))
            return;

        _targets.Clear();
        _lookup.GetEntitiesInRange(Transform(ent).Coordinates, args.Effect.Range, _targets);

        Entity<MobStateComponent>? victim = null;
        foreach (var target in _targets)
        {
            if (target.Owner == ent.Owner || _mobState.IsDead(target))
                continue;

            victim = target;
            break;
        }

        if (victim == null)
            return;

        _popup.PopupEntity(Loc.GetString("adt-symptom-aggression-attack", ("name", Name(ent))), ent, PopupType.MediumCaution);

        var weapon = _hands.GetActiveItem(ent.Owner);
        var weaponUid = weapon ?? ent.Owner;

        if (!TryComp<MeleeWeaponComponent>(weaponUid, out var melee))
            return;

        var wasCombat = TryComp<CombatModeComponent>(ent, out var combat) && combat.IsInCombatMode;
        if (combat != null)
            _combat.SetInCombatMode(ent.Owner, true, combat);

        _melee.AttemptLightAttack(ent.Owner, weaponUid, melee, victim.Value.Owner);
        ShowLungeToSelf(ent.Owner, weaponUid, melee, victim.Value.Owner);

        if (combat != null)
            _combat.SetInCombatMode(ent.Owner, wasCombat, combat);
    }

    private void ShowLungeToSelf(EntityUid user, EntityUid weapon, MeleeWeaponComponent melee, EntityUid target)
    {
        var userXform = Transform(user);
        var targetPos = _transform.GetMapCoordinates(target).Position;
        var localPos = Vector2.Transform(targetPos, _transform.GetInvWorldMatrix(userXform));

        if (localPos.LengthSquared() <= 0f)
            return;

        localPos = userXform.LocalRotation.RotateVec(localPos);

        var visualLength = melee.Range - 0.2f;
        if (localPos.Length() > visualLength)
            localPos = localPos.Normalized() * visualLength;

        RaiseNetworkEvent(
            new MeleeLungeEvent(GetNetEntity(user), GetNetEntity(weapon), melee.Angle, localPos, melee.Animation),
            user);
    }
}
