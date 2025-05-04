using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Fishing.Components;
using Content.Shared.ADT.Fishing.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Fishing;

public sealed class FishingSystem : SharedFishingSystem
{
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FishingLureComponent, StartCollideEvent>(OnFloatCollide);
        SubscribeLocalEvent<FishingRodComponent, UseInHandEvent>(OnFishingInteract);
    }

    private void OnFishingInteract(EntityUid uid, FishingRodComponent component, UseInHandEvent args)
    {
        if (!FisherQuery.TryComp(args.User, out var fisherComp) || fisherComp.TotalProgress == null || args.Handled || !Timing.IsFirstTimePredicted)
            return;

        fisherComp.TotalProgress += fisherComp.ProgressPerUse * component.Efficiency;
        Dirty(args.User, fisherComp);

        args.Handled = true;
    }

    private void OnFloatCollide(Entity<FishingLureComponent> ent, ref StartCollideEvent args)
    {
        var attachedEnt = args.OtherEntity;

        if (HasComp<ActiveFishingSpotComponent>(attachedEnt))
            return;

        if (!FishSpotQuery.TryComp(attachedEnt, out var spotComp))
        {
            if (args.OtherBody.BodyType == BodyType.Static)
                return;

            Anchor(ent, attachedEnt);
            return;
        }

        Anchor(ent, attachedEnt);

        var fish = spotComp.FishList.GetSpawns(_random.GetRandom(), EntityManager, _proto).First();

        _proto.Index(fish).TryGetComponent(out FishComponent? fishComp, _compFactory);

        var activeFishSpot = EnsureComp<ActiveFishingSpotComponent>(attachedEnt);
        activeFishSpot.Fish = fish;
        activeFishSpot.FishDifficulty = fishComp?.FishDifficulty ?? FishComponent.DefaultDifficulty;

        var time = spotComp.FishDefaultTimer + _random.NextFloat(-spotComp.FishTimerVariety, spotComp.FishTimerVariety);
        activeFishSpot.FishingStartTime = Timing.CurTime + TimeSpan.FromSeconds(time);
        activeFishSpot.AttachedFishingLure = ent;

        Dirty(attachedEnt, activeFishSpot);
        Dirty(ent);
    }

    private void Anchor(Entity<FishingLureComponent> ent, EntityUid attachedEnt)
    {
        var spotPosition = Xform.GetWorldPosition(attachedEnt);
        Xform.SetWorldPosition(ent, spotPosition);
        Xform.SetParent(ent, attachedEnt);
        _physics.SetLinearVelocity(ent, Vector2.Zero);
        _physics.SetAngularVelocity(ent, 0f);
        ent.Comp.AttachedEntity = attachedEnt;
        RemComp<ItemComponent>(ent);
        RemComp<PullableComponent>(ent);
    }

    protected override void StopFishing(
        Entity<FishingRodComponent> fishingRod,
        EntityUid? fisher,
        EntityUid? attachedEnt)
    {
        QueueDel(fishingRod.Comp.FishingLure);
        base.StopFishing(fishingRod, fisher, attachedEnt);
    }

    protected override void SetupFishingFloat(Entity<FishingRodComponent> fishingRod, EntityUid player, EntityCoordinates target)
    {
        var (uid, component) = fishingRod;
        var targetCoords = Xform.ToMapCoordinates(target);
        var playerCoords = Xform.GetMapCoordinates(Transform(player));

        var fishFloat = Spawn(component.FloatPrototype, playerCoords);
        component.FishingLure = fishFloat;
        Dirty(uid, component);

        var direction = targetCoords.Position - playerCoords.Position;
        if (direction == Vector2.Zero)
            direction = Vector2.UnitX;

        Throwing.TryThrow(fishFloat, direction, 15f, player, 2f, null, true);

        var fishLureComp = EnsureComp<FishingLureComponent>(fishFloat);
        fishLureComp.FishingRod = uid;
        Dirty(fishFloat, fishLureComp);

        var visuals = EnsureComp<JointVisualsComponent>(fishFloat);
        visuals.Sprite = component.RopeSprite;
        visuals.OffsetA = component.RopeLureOffset;
        visuals.OffsetB = component.RopeUserOffset;
        visuals.Target = GetNetEntity(uid);
    }

    protected override void ThrowFishReward(EntProtoId fishId, EntityUid fishSpot, EntityUid target)
    {
        var position = Transform(fishSpot).Coordinates;
        var fish = Spawn(fishId, position);
        var direction = Xform.GetWorldPosition(target) - Xform.GetWorldPosition(fish);
        var length = direction.Length();
        var distance = Math.Clamp(length, 0.5f, 15f);
        direction *= distance / length;

        Throwing.TryThrow(fish, direction, 7f);
    }

    protected override void CalculateFightingTimings(Entity<ActiveFisherComponent> fisher, ActiveFishingSpotComponent activeSpotComp)
    {
        if (Timing.CurTime < fisher.Comp.NextStruggle)
            return;

        fisher.Comp.NextStruggle = Timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(0.06f, 0.18f));
        fisher.Comp.TotalProgress -= activeSpotComp.FishDifficulty;
        Dirty(fisher);
    }
}
