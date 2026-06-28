using Content.Server.Chat.Systems;
using Content.Server.NPC;
using Content.Server.NPC.Systems;
using Content.Server.Pinpointer;
using Content.Shared.Dragon;
using Content.Shared.Examine;
using Content.Shared.Sprite;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;
using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using Robust.Shared.Random;

namespace Content.Server.Dragon;

/// <summary>
/// Handles events for rift entities and rift updating.
/// </summary>
public sealed class DragonRiftSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DragonSystem _dragon = default!;
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DragonRiftComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<DragonRiftComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<DragonRiftComponent, AnchorStateChangedEvent>(OnAnchorChange);
        SubscribeLocalEvent<DragonRiftComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnGetState(Entity<DragonRiftComponent> ent, ref ComponentGetState args)
    {
        args.State = new DragonRiftComponentState
        {
            State = ent.Comp.State,
        };
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DragonRiftComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            if (comp.State != DragonRiftState.Finished && comp.Accumulator >= comp.MaxAccumulator)
            {
                if (comp.Dragon != null)
                    _dragon.RiftCharged(comp.Dragon.Value);

                comp.Accumulator = comp.MaxAccumulator;
                RemComp<DamageableComponent>(uid);
                comp.State = DragonRiftState.Finished;
                Dirty(uid, comp);
            }
            else if (comp.State != DragonRiftState.Finished)
            {
                comp.Accumulator += frameTime;
            }

            if (comp.State < DragonRiftState.AlmostFinished && comp.Accumulator > comp.MaxAccumulator / 2f)
            {
                comp.State = DragonRiftState.AlmostFinished;
                Dirty(uid, comp);

                var msg = Loc.GetString("carp-rift-warning",
                    ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((uid, xform)))));
                _chat.DispatchGlobalAnnouncement(msg, playSound: false, colorOverride: Color.Red);
                _audio.PlayGlobal("/Audio/Misc/notice1.ogg", Filter.Broadcast(), true);
                _navMap.SetBeaconEnabled(uid, true);
            }

            comp.SpawnAccumulator += frameTime;
            if (comp.SpawnAccumulator > comp.SpawnCooldown)
            {
                string? proto = null;

                if (!string.IsNullOrEmpty(comp.SpawnPrototypeStrong) && _random.Prob(comp.StrongSpawnChance))
                {
                    proto = comp.SpawnPrototypeStrong;
                }
                else if (!string.IsNullOrEmpty(comp.SpawnPrototype))
                {
                    proto = comp.SpawnPrototype;
                }
                else if (comp.SpawnPrototypes is { Count: > 0 })
                {
                    proto = _random.Pick(comp.SpawnPrototypes);
                }

                if (proto != null)
                {
                    var lowerProto = proto.ToLower();
                    if (lowerProto.Contains("carp"))
                    {
                        if (_random.Prob(0.35f))
                        {
                            proto = "MobSharkDragon";
                        }
                    }
                }
                // ------------------------------------------

                if (proto != null)
                {
                    comp.SpawnAccumulator -= comp.SpawnCooldown;
                    var ent = Spawn(proto, xform.Coordinates);

                    if (TryComp<RandomSpriteComponent>(comp.Dragon, out var randomSprite))
                    {
                        var spawnedSprite = EnsureComp<RandomSpriteComponent>(ent);
                        _serManager.CopyTo(randomSprite, ref spawnedSprite, notNullableOverride: true);
                        Dirty(ent, spawnedSprite);
                    }

                    if (comp.Dragon != null)
                        _npc.SetBlackboard(ent, NPCBlackboard.FollowTarget, new EntityCoordinates(comp.Dragon.Value, Vector2.Zero));
                }
            }
        }
    }

    private void OnExamined(EntityUid uid, DragonRiftComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("carp-rift-examine", ("percentage", MathF.Round(component.Accumulator / component.MaxAccumulator * 100))));
    }

    private void OnAnchorChange(EntityUid uid, DragonRiftComponent component, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored && component.State == DragonRiftState.Charging)
        {
            QueueDel(uid);
        }
    }

    private void OnShutdown(EntityUid uid, DragonRiftComponent comp, ComponentShutdown args)
    {
        if (!TryComp<DragonComponent>(comp.Dragon, out var dragon) || dragon.Weakened)
            return;

        _dragon.RiftDestroyed(comp.Dragon.Value, dragon);
    }
}