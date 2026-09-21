using Content.Shared.ADT.Fishing;
using Content.Shared.ADT.Fishing.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Fishing;

public sealed class ADTFishingMinigameSystem : EntitySystem
{
    [Dependency] private readonly ADTFishingSystem _fishing = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private static readonly SoundSpecifier BiteSound = new SoundPathSpecifier("/Audio/ADT/Items/Fishing/fishing_rod_reel.ogg");

    private const float BreakDistance = 4f;

    private const float HookAcceleration = 2.1f;
    private const float HookGravity = 1.5f;
    private const float HookMaxSpeed = 1.1f;
    private const float HookBounce = 0.35f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTFishingMinigameComponent, ADTFishingHoldMessage>(OnHold);
        SubscribeLocalEvent<ADTFishingMinigameComponent, BoundUIClosedEvent>(OnUiClosed);
    }

    public void Start(Entity<ADTFishingRodComponent> rod, EntityUid user, Entity<ADTFishingSpotComponent> spot, EntProtoId fish)
    {
        var difficulty = 0.35f;

        if (_proto.TryIndex(fish, out var fishProto) &&
            fishProto.TryGetComponent<ADTFishComponent>(out var fishComp, _compFactory))
        {
            difficulty = fishComp.Difficulty;
        }

        var comp = EnsureComp<ADTFishingMinigameComponent>(rod.Owner);

        comp.Fish = fish;
        comp.User = user;
        comp.Spot = spot.Owner;
        comp.Difficulty = Math.Clamp(difficulty, 0f, 1f);
        comp.Efficiency = rod.Comp.Efficiency;
        comp.HookSize = rod.Comp.HookSize;
        comp.FishPosition = _random.NextFloat(0.25f, 0.75f);
        comp.FishTarget = comp.FishPosition;
        comp.NextFishMove = _timing.CurTime;
        comp.HookPosition = 0.5f;
        comp.HookVelocity = 0f;
        comp.Progress = 0.4f;
        comp.Holding = false;
        comp.Hooked = true;

        Dirty(rod.Owner, comp);

        _audio.PlayPvs(BiteSound, rod.Owner);
        _popup.PopupEntity(Loc.GetString("adt-fishing-bite"), rod.Owner, user, PopupType.Medium);
        _ui.TryOpenUi(rod.Owner, ADTFishingUiKey.Key, user);
    }

    private void OnHold(Entity<ADTFishingMinigameComponent> ent, ref ADTFishingHoldMessage args)
    {
        if (!ent.Comp.Hooked || args.Actor != ent.Comp.User)
            return;

        ent.Comp.Holding = args.Holding;
        Dirty(ent);
    }

    private void OnUiClosed(Entity<ADTFishingMinigameComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!ent.Comp.Hooked || !Equals(args.UiKey, ADTFishingUiKey.Key) || args.Actor != ent.Comp.User)
            return;

        Fail(ent, "adt-fishing-escaped");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ADTFishingMinigameComponent, ADTFishingRodComponent>();

        while (query.MoveNext(out var uid, out var minigame, out var rod))
        {
            if (!minigame.Hooked)
                continue;

            var ent = new Entity<ADTFishingMinigameComponent>(uid, minigame);

            if (!IsValid(ent))
            {
                Fail(ent, "adt-fishing-escaped");
                continue;
            }

            MoveFish(ent, frameTime);
            MoveHook(ent, frameTime);

            var half = minigame.HookSize / 2f;
            var onTarget = Math.Abs(minigame.FishPosition - minigame.HookPosition) <= half;

            if (onTarget)
                minigame.Progress += 0.30f * minigame.Efficiency * frameTime;
            else
                minigame.Progress -= 0.20f * (0.7f + minigame.Difficulty) * frameTime;

            minigame.Progress = Math.Clamp(minigame.Progress, 0f, 1f);
            Dirty(uid, minigame);

            if (minigame.Progress >= 1f)
            {
                Succeed((uid, minigame), (uid, rod));
                continue;
            }

            if (minigame.Progress <= 0f)
                Fail(ent, "adt-fishing-escaped");
        }
    }

    private bool IsValid(Entity<ADTFishingMinigameComponent> ent)
    {
        if (TerminatingOrDeleted(ent.Comp.User) || TerminatingOrDeleted(ent.Comp.Spot))
            return false;

        if (!_hands.IsHolding(ent.Comp.User, ent.Owner))
            return false;

        var userPos = _transform.GetMapCoordinates(ent.Comp.User);
        var spotPos = _transform.GetMapCoordinates(ent.Comp.Spot);

        if (userPos.MapId != spotPos.MapId)
            return false;

        return (userPos.Position - spotPos.Position).Length() <= BreakDistance;
    }

    private void MoveFish(Entity<ADTFishingMinigameComponent> ent, float frameTime)
    {
        var comp = ent.Comp;

        if (_timing.CurTime >= comp.NextFishMove)
        {
            var jump = 0.15f + comp.Difficulty * 0.45f;
            comp.FishTarget = Math.Clamp(comp.FishPosition + _random.NextFloat(-jump, jump) * 2f, 0.05f, 0.95f);

            var delay = _random.NextFloat(0.5f, 1.6f) / (0.6f + comp.Difficulty);
            comp.NextFishMove = _timing.CurTime + TimeSpan.FromSeconds(delay);
        }

        var speed = (0.3f + comp.Difficulty * 0.9f) * frameTime;
        var diff = comp.FishTarget - comp.FishPosition;

        if (Math.Abs(diff) <= speed)
            comp.FishPosition = comp.FishTarget;
        else
            comp.FishPosition += Math.Sign(diff) * speed;
    }

    private void MoveHook(Entity<ADTFishingMinigameComponent> ent, float frameTime)
    {
        var comp = ent.Comp;
        var accel = comp.Holding ? HookAcceleration : -HookGravity;

        comp.HookVelocity = Math.Clamp(comp.HookVelocity + accel * frameTime, -HookMaxSpeed, HookMaxSpeed);
        comp.HookPosition += comp.HookVelocity * frameTime;

        var half = comp.HookSize / 2f;

        if (comp.HookPosition < half)
        {
            comp.HookPosition = half;
            comp.HookVelocity = -comp.HookVelocity * HookBounce;
        }
        else if (comp.HookPosition > 1f - half)
        {
            comp.HookPosition = 1f - half;
            comp.HookVelocity = -comp.HookVelocity * HookBounce;
        }
    }

    private void Succeed(Entity<ADTFishingMinigameComponent> ent, Entity<ADTFishingRodComponent> rod)
    {
        var user = ent.Comp.User;
        var spot = ent.Comp.Spot;
        var fishProto = ent.Comp.Fish;

        Stop(ent, rod);

        var fish = Spawn(fishProto, Transform(spot).Coordinates);

        _hands.PickupOrDrop(user, fish);
        _audio.PlayPvs(rod.Comp.CatchSound, rod.Owner);
        _popup.PopupEntity(Loc.GetString("adt-fishing-caught", ("fish", fish)), rod.Owner, user);
    }

    private void Fail(Entity<ADTFishingMinigameComponent> ent, string message)
    {
        var user = ent.Comp.User;

        if (!TryComp<ADTFishingRodComponent>(ent.Owner, out var rod))
        {
            RemCompDeferred<ADTFishingMinigameComponent>(ent.Owner);
            return;
        }

        Stop(ent, (ent.Owner, rod));

        if (!TerminatingOrDeleted(user))
            _popup.PopupEntity(Loc.GetString(message), ent.Owner, user, PopupType.MediumCaution);
    }

    private void Stop(Entity<ADTFishingMinigameComponent> ent, Entity<ADTFishingRodComponent> rod)
    {
        var spot = ent.Comp.Spot;

        ent.Comp.Hooked = false;
        RemCompDeferred<ADTFishingMinigameComponent>(ent.Owner);
        _ui.CloseUi(ent.Owner, ADTFishingUiKey.Key, ent.Comp.User);
        _fishing.EndCast(rod, spot);
    }
}
