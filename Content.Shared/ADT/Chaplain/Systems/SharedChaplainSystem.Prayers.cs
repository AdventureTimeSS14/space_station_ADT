using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.Chaplain.Prayers;
using Content.Shared.ADT.Chaplain.Prototypes;
using Content.Shared.ADT.Chat;
using Content.Shared.ADT.Damage.Systems;
using Content.Shared.ADT.HolyDamage;
using Content.Shared.Chat;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Speech;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Chaplain.Systems;

public abstract partial class SharedChaplainSystem
{
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private GroupHealSystem _groupHeal = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private ThrowingSystem _throwing = default!;

    private static readonly ProtoId<SpeechVerbPrototype> PrayerSpeechVerb = "ADTPrayer";
    private static readonly EntProtoId RepelSlowdown = "BasicSlowdownStatusEffect";
    private const int MinHintLength = 2;
    private static readonly char[] PrayerPunctuation = { '.', '!', '?', ',', ' ' };

    private readonly HashSet<Entity<HolyAffectedComponent>> _repelTargets = new();

    private void InitializePrayers()
    {
        SubscribeLocalEvent<ChaplainComponent, GetSpeechVerbOverrideEvent>(OnChaplainSpeechVerb);
        SubscribeLocalEvent<ChaplainFollowerComponent, GetSpeechVerbOverrideEvent>(OnFollowerSpeechVerb);
        SubscribeLocalEvent<ChaplainComponent, EntitySpokeEvent>(OnChaplainSpoke);
        SubscribeLocalEvent<ChaplainFollowerComponent, EntitySpokeEvent>(OnFollowerSpoke);
    }

    private void OnChaplainSpeechVerb(Entity<ChaplainComponent> ent, ref GetSpeechVerbOverrideEvent args)
    {
        if (TryMatchPrayer(ent, args.Message, out _))
            args.SpeechVerb = PrayerSpeechVerb;
    }

    private void OnFollowerSpeechVerb(Entity<ChaplainFollowerComponent> ent, ref GetSpeechVerbOverrideEvent args)
    {
        if (TryMatchPrayer(ent, args.Message, out _))
            args.SpeechVerb = PrayerSpeechVerb;
    }

    private void OnChaplainSpoke(Entity<ChaplainComponent> ent, ref EntitySpokeEvent args)
    {
        if (args.Channel != null || !TryMatchPrayer(ent, args.OriginalMessage, out var prayer))
            return;

        if (!TryStartPrayer(ent, ent.Comp.PrayerCooldowns, prayer))
            return;

        if (!TryUseFaith(ent, prayer.Cost))
        {
            ent.Comp.PrayerCooldowns.Remove(prayer.ID);
            return;
        }

        ApplyPrayer(ent, ent, prayer);
    }

    private void OnFollowerSpoke(Entity<ChaplainFollowerComponent> ent, ref EntitySpokeEvent args)
    {
        if (args.Channel != null || !TryMatchPrayer(ent, args.OriginalMessage, out var prayer))
            return;

        if (ent.Comp.Chaplain is not { } chaplain || !TryComp<ChaplainComponent>(chaplain, out var chaplainComp) || _mobState.IsDead(chaplain))
        {
            _popup.PopupEntity(Loc.GetString("chaplain-prayer-no-chaplain"), ent, ent);
            return;
        }

        if (prayer.ChaplainRange > 0 && !_transform.InRange(ent.Owner, chaplain, prayer.ChaplainRange))
        {
            _popup.PopupEntity(Loc.GetString("chaplain-prayer-chaplain-far"), ent, ent);
            return;
        }

        if (!TryStartPrayer(ent, ent.Comp.PrayerCooldowns, prayer))
            return;

        ApplyPrayer(ent, (chaplain, chaplainComp), prayer);
    }

    private bool TryStartPrayer(EntityUid user, Dictionary<ProtoId<ChaplainPrayerPrototype>, TimeSpan> cooldowns, ChaplainPrayerPrototype prayer)
    {
        var now = _timing.CurTime;
        if (cooldowns.TryGetValue(prayer.ID, out var next) && now < next)
        {
            _popup.PopupEntity(Loc.GetString("chaplain-prayer-cooldown"), user, user);
            return false;
        }

        cooldowns[prayer.ID] = now + prayer.Cooldown;
        return true;
    }

    private void ApplyPrayer(EntityUid user, Entity<ChaplainComponent> chaplain, ChaplainPrayerPrototype prayer)
    {
        if (prayer.Effect is { } effect)
            SpawnAttachedTo(effect, Transform(user).Coordinates);

        if (prayer.Sound is { } sound)
            _audio.PlayPvs(sound, user);

        foreach (var prayerEffect in prayer.Effects)
        {
            switch (prayerEffect)
            {
                case HealPrayerEffect heal:
                    _groupHeal.Heal(user, null, heal.Heal, origin: user);
                    break;

                case FaithPrayerEffect faith:
                    ChangeFaith((chaplain.Owner, chaplain.Comp), faith.Amount);
                    break;

                case RepelPrayerEffect repel:
                    Repel(user, repel);
                    break;

                case ShieldPrayerEffect shield:
                    GrantHolyShield(user, shield.Duration);
                    break;
            }
        }
    }

    private void Repel(EntityUid user, RepelPrayerEffect repel)
    {
        var userPos = _transform.GetWorldPosition(user);
        var multiplier = GetHolyMultiplier(user);

        _repelTargets.Clear();
        _lookup.GetEntitiesInRange(Transform(user).Coordinates, repel.Range, _repelTargets);

        foreach (var target in _repelTargets)
        {
            if (target.Owner == user || !HasComp<MobStateComponent>(target) || _mobState.IsDead(target))
                continue;

            var direction = _transform.GetWorldPosition(target) - userPos;
            if (direction.LengthSquared() < 0.01f)
                direction = Vector2.UnitX;

            _throwing.TryThrow(target, direction.Normalized() * repel.ThrowDistance * multiplier, user: user, recoil: false);
            _movementMod.TryUpdateMovementSpeedModDuration(target, RepelSlowdown, repel.Slowdown, repel.SlowdownMultiplier);

            if (target.Comp.Suppressible)
                AddSuppression(target, repel.Suppression * multiplier);
        }
    }

    public bool TryMatchPrayer(EntityUid speaker, string message, [NotNullWhen(true)] out ChaplainPrayerPrototype? prayer)
    {
        prayer = null;
        var normalized = NormalizePrayer(message);
        if (normalized.Length == 0)
            return false;

        foreach (var proto in _proto.EnumeratePrototypes<ChaplainPrayerPrototype>())
        {
            if (!CanUsePrayer(speaker, proto) || !string.Equals(NormalizePrayer(proto.Text), normalized, StringComparison.OrdinalIgnoreCase))
                continue;

            prayer = proto;
            return true;
        }

        return false;
    }

    public string? GetPrayerCompletion(EntityUid speaker, string typed)
    {
        var trimmed = typed.TrimStart();
        if (trimmed.Length < MinHintLength)
            return null;

        foreach (var proto in _proto.EnumeratePrototypes<ChaplainPrayerPrototype>())
        {
            if (!CanUsePrayer(speaker, proto))
                continue;

            if (proto.Text.Length > trimmed.Length && proto.Text.StartsWith(trimmed, StringComparison.OrdinalIgnoreCase))
                return proto.Text.Substring(trimmed.Length);
        }

        return null;
    }

    private bool CanUsePrayer(EntityUid speaker, ChaplainPrayerPrototype prayer)
    {
        return prayer.Role switch
        {
            ChaplainPrayerRole.Chaplain => HasComp<ChaplainComponent>(speaker),
            ChaplainPrayerRole.Follower => HasComp<ChaplainFollowerComponent>(speaker),
            _ => false,
        };
    }

    private static string NormalizePrayer(string text)
    {
        return text.Trim().TrimEnd(PrayerPunctuation);
    }
}
