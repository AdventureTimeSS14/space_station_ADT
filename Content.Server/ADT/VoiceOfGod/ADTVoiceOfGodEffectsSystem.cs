using System.Linq;
using System.Numerics;
using Content.Server.ADT.Hallucinations;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Silicons.Laws;
using Content.Shared.ADT.VoiceOfGod;
using Content.Shared.Atmos.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Medical;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using NewStatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;

namespace Content.Server.ADT.VoiceOfGod;

public sealed partial class ADTStunEffectSystem : EntityEffectSystem<MetaDataComponent, ADTStun>
{
    [Dependency] private SharedStunSystem _stun = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ADTStun> args)
    {
        _stun.TryUpdateStunDuration(entity.Owner, args.Effect.Time * args.Scale);
    }
}

public sealed partial class ADTKnockdownEffectSystem : EntityEffectSystem<MetaDataComponent, ADTKnockdown>
{
    [Dependency] private SharedStunSystem _stun = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ADTKnockdown> args)
    {
        var time = args.Effect.Scaled
            ? args.Effect.Time * args.Scale
            : args.Effect.Time;

        _stun.TryKnockdown(entity.Owner, time, autoStand: args.Effect.AutoStand, drop: args.Effect.Drop);
    }
}

public sealed partial class ADTStandUpEffectSystem : EntityEffectSystem<MetaDataComponent, ADTStandUp>
{
    [Dependency] private SharedStunSystem _stun = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ADTStandUp> args)
    {
        _stun.TryUnstun(entity.Owner);
        _stun.TryUpdateParalyzeDuration(entity.Owner, TimeSpan.Zero);
        _stun.ForceStandUp(entity.Owner);
    }
}

public sealed partial class ADTWakeUpEffectSystem : EntityEffectSystem<MetaDataComponent, ADTWakeUp>
{
    [Dependency] private NewStatusEffectsSystem _statusEffects = default!;
    [Dependency] private SleepingSystem _sleeping = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ADTWakeUp> args)
    {
        _statusEffects.TryRemoveStatusEffect(entity.Owner, args.Effect.SleepEffect);
        _sleeping.TryWaking(entity.Owner, true);
    }
}

public sealed partial class ADTForceVomitEffectSystem : EntityEffectSystem<MetaDataComponent, ADTForceVomit>
{
    [Dependency] private VomitSystem _vomit = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ADTForceVomit> args)
    {
        _vomit.Vomit(entity.Owner, force: true);
    }
}

public sealed partial class ADTHallucinateEffectSystem : EntityEffectSystem<MetaDataComponent, ADTHallucinate>
{
    [Dependency] private HallucinationsSystem _hallucinations = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ADTHallucinate> args)
    {
        _hallucinations.StartHallucinations(entity.Owner, args.Effect.Key, args.Effect.Time * args.Scale, true, args.Effect.Pack);
    }
}

public sealed partial class ADTDamageFromUserEffectSystem : EntityEffectSystem<DamageableComponent, ADTDamageFromUser>
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<ADTDamageFromUser> args)
    {
        var damage = args.Effect.Damage is { } specifier
            ? specifier * args.Scale
            : new DamageSpecifier();

        foreach (var (group, amount) in args.Effect.Groups)
        {
            damage += new DamageSpecifier(_proto.Index(group), amount * args.Scale);
        }

        _damageable.TryChangeDamage(entity.AsNullable(), damage, args.Effect.IgnoreResistances, origin: args.User);
    }
}

public sealed partial class ADTFireStacksEffectSystem : EntityEffectSystem<FlammableComponent, ADTFireStacks>
{
    [Dependency] private FlammableSystem _flammable = default!;

    protected override void Effect(Entity<FlammableComponent> entity, ref EntityEffectEvent<ADTFireStacks> args)
    {
        _flammable.AdjustFireStacks(entity.Owner, args.Effect.Stacks * args.Scale, entity.Comp);

        if (args.User is { } user)
            _flammable.Ignite(entity.Owner, user, entity.Comp, user);
    }
}

public sealed partial class ADTRepulseFromUserEffectSystem : EntityEffectSystem<TransformComponent, ADTRepulseFromUser>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ADTRepulseFromUser> args)
    {
        if (args.User is not { } user)
            return;

        var direction = _transform.GetMapCoordinates(entity.Owner).Position - _transform.GetMapCoordinates(user).Position;

        if (direction.LengthSquared() < 0.01f)
            direction = _random.NextAngle().ToVec();

        _throwing.TryThrow(entity.Owner, Vector2.Normalize(direction) * args.Effect.Distance * args.Scale, args.Effect.Speed, user);
    }
}

public sealed partial class ADTRandomStepEffectSystem : EntityEffectSystem<TransformComponent, ADTRandomStep>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    private static readonly Vector2[] Steps = { Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY };

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ADTRandomStep> args)
    {
        _throwing.TryThrow(entity.Owner, _random.Pick(Steps), args.Effect.Speed, args.User, 0, null, false, false, false, false, false);
    }
}

public sealed partial class ADTForceSayEffectSystem : EntityEffectSystem<MetaDataComponent, ADTForceSay>
{
    [Dependency] private ChatSystem _chat = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ADTForceSay> args)
    {
        var user = args.User is { } speaker ? Name(speaker) : string.Empty;
        var message = Loc.GetString(args.Effect.Message, ("target", entity.Comp.EntityName), ("user", user));

        _chat.TrySendInGameICMessage(entity.Owner, message, InGameICChatType.Speak, false, ignoreActionBlocker: true);
    }
}

public sealed partial class ADTStateLawsEffectSystem : EntityEffectSystem<SiliconLawBoundComponent, ADTStateLaws>
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SiliconLawSystem _laws = default!;

    protected override void Effect(Entity<SiliconLawBoundComponent> entity, ref EntityEffectEvent<ADTStateLaws> args)
    {
        foreach (var law in _laws.GetLaws(entity.Owner, entity.Comp).Laws.OrderBy(law => law.Order))
        {
            _chat.TrySendInGameICMessage(entity.Owner, $"{law.Order}. {Loc.GetString(law.LawString)}", InGameICChatType.Speak, false, ignoreActionBlocker: true);
        }
    }
}

public sealed partial class ADTForceEmoteEffectSystem : EntityEffectSystem<MetaDataComponent, ADTForceEmote>
{
    [Dependency] private ChatSystem _chat = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ADTForceEmote> args)
    {
        _chat.TryEmoteWithChat(entity.Owner, args.Effect.Emote, ignoreActionBlocker: true, forceEmote: true);
    }
}

public sealed partial class ADTSetCombatModeEffectSystem : EntityEffectSystem<CombatModeComponent, ADTSetCombatMode>
{
    [Dependency] private SharedCombatModeSystem _combat = default!;

    protected override void Effect(Entity<CombatModeComponent> entity, ref EntityEffectEvent<ADTSetCombatMode> args)
    {
        _combat.SetInCombatMode(entity.Owner, args.Effect.Enabled, entity.Comp);
    }
}

public sealed partial class ADTBuckleNearbyEffectSystem : EntityEffectSystem<BuckleComponent, ADTBuckleNearby>
{
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;

    protected override void Effect(Entity<BuckleComponent> entity, ref EntityEffectEvent<ADTBuckleNearby> args)
    {
        foreach (var strap in _lookup.GetEntitiesInRange<StrapComponent>(Transform(entity.Owner).Coordinates, args.Effect.Range))
        {
            if (_buckle.TryBuckle(entity.Owner, entity.Owner, strap, popup: false))
                break;
        }
    }
}

public sealed partial class ADTUnbuckleEffectSystem : EntityEffectSystem<BuckleComponent, ADTUnbuckle>
{
    [Dependency] private SharedBuckleSystem _buckle = default!;

    protected override void Effect(Entity<BuckleComponent> entity, ref EntityEffectEvent<ADTUnbuckle> args)
    {
        if (entity.Comp.Buckled)
            _buckle.TryUnbuckle(entity.AsNullable(), entity.Owner, false);
    }
}
