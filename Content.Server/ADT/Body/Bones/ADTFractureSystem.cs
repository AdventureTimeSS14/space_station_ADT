using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.Body.Bones;
using Content.Shared.ADT.Traits.Assorted;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Cuffs;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Explosion;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Body.Bones;

public sealed partial class ADTFractureSystem : SharedADTBoneSystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedCuffableSystem _cuffable = default!;

    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> Slash = "Slash";
    private static readonly ProtoId<DamageTypePrototype> Piercing = "Piercing";
    private static readonly ProtoId<EmotePrototype> ScreamEmote = "Scream";
    private static readonly SoundSpecifier BreakSound = new SoundCollectionSpecifier("ADTBoneBreak", AudioParams.Default.WithVolume(4f));

    private bool _relocking;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBonesComponent, BeforeDamageChangedEvent>(OnBeforeDamage);
        SubscribeLocalEvent<ADTBonesComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<ADTBonesComponent, BeforeExplodeEvent>(OnBeforeExplode);
        SubscribeLocalEvent<ADTBonesComponent, RejuvenateEvent>(OnRejuvenate, before: new[] { typeof(DamageableSystem) });

        InitializeEffects();
        InitializeSplints();
    }

    private void OnBeforeDamage(Entity<ADTBonesComponent> ent, ref BeforeDamageChangedEvent args)
    {
        ent.Comp.PendingZone = null;
        ent.Comp.PendingSourceBrute = 0f;

        if (args.Origin is not { } origin || !TryComp<ADTFractureSourceComponent>(origin, out var source))
            return;

        var brute = GetBruteSpecifier(args.Damage);
        if (brute.Empty)
            return;

        var zone = PickZone(source.ZoneWeights);
        if (zone == null)
            return;

        brute = ApplyZoneArmor(ent, zone.Value, brute);
        ent.Comp.PendingZone = zone;
        ent.Comp.PendingSourceBrute = brute.GetTotal().Float() * source.DamageMultiplier;
    }

    private void OnDamageChanged(Entity<ADTBonesComponent> ent, ref DamageChangedEvent args)
    {
        if (ent.Comp.PendingZone is { } zone)
        {
            var damage = ent.Comp.PendingSourceBrute;
            ent.Comp.PendingZone = null;
            ent.Comp.PendingSourceBrute = 0f;

            if (args.DamageIncreased && args.Origin != null)
                HandleZoneHit(ent, zone, damage);
        }

        if (!args.DamageIncreased && args.DamageDelta != null)
            Relock(ent.Owner);
    }

    private void OnBeforeExplode(Entity<ADTBonesComponent> ent, ref BeforeExplodeEvent args)
    {
        var damage = GetBruteSpecifier(args.Damage).GetTotal().Float() * ent.Comp.ExplosionLimbFactor;
        if (damage < ent.Comp.MinBreakDamage)
            return;

        foreach (var (_, category) in EnumerateBones(ent).ToList())
        {
            if (category is { } zone)
                HandleZoneHit(ent, zone, damage);
        }
    }

    private void OnRejuvenate(Entity<ADTBonesComponent> ent, ref RejuvenateEvent args)
    {
        MendAll(ent);
    }

    private void HandleZoneHit(Entity<ADTBonesComponent> ent, ProtoId<OrganCategoryPrototype> zone, float damage)
    {
        if (TryGetBone(ent, zone, out var bone))
        {
            if (bone.Value.Comp.Fracture != null && _random.Prob(ent.Comp.ScreamOnHitChance))
                TryScream(ent);

            if (bone.Value.Comp.Splinted && _random.Prob(Math.Clamp(damage * ent.Comp.SplintBreakChancePerDamage, 0f, 1f)))
                RemoveSplint(ent, bone.Value, true);
        }

        TryFracture(ent, zone, damage);
    }

    public bool TryFracture(EntityUid body, ProtoId<OrganCategoryPrototype> zone, float damage)
    {
        if (!TryComp<ADTBonesComponent>(body, out var bones) || !bones.CanBreak || HasComp<GodmodeComponent>(body))
            return false;

        if (damage < bones.MinBreakDamage || !TryGetBone(body, zone, out var bone) || !CanBreak(bone.Value, bone.Value.Comp))
            return false;

        var accumulated = GetBodyBrute(body) * bones.AccumulatedFactor;
        if (accumulated + damage <= bone.Value.Comp.MinBrokenDamage)
            return false;

        if (!_random.Prob(Math.Clamp(damage * bones.Fragility / 100f, 0f, 1f)))
            return false;

        if (PickType(damage) is not { } type)
            return false;

        return ApplyFracture(body, bone.Value, type);
    }

    public bool ApplyFracture(EntityUid body, Entity<ADTBoneComponent> bone, ADTFractureTypePrototype type, bool silent = false)
    {
        if (!CanBreak(bone, bone.Comp))
            return false;

        if (GetFractureType(bone.Comp) is { } existing && existing.Power >= type.Power)
            return false;

        SetFracture(bone, type.ID);

        if (bone.Comp.Splinted && !type.CanSplint)
            SetSplinted(bone, false, 0f);

        SetLockedDamage(bone, Math.Max(bone.Comp.LockedDamage, type.LockedDamage));

        var fractured = EnsureComp<ADTFracturedComponent>(body);
        if (TryComp<ADTBonesComponent>(body, out var bones))
            SetIgnoreUntil((body, fractured), Timing.CurTime + bones.IgnoreDuration);

        Refresh(body);

        if (type.BleedAmount > 0f)
            _bloodstream.TryModifyBleedAmount(body, type.BleedAmount);

        _adminLog.Add(LogType.Damaged, LogImpact.Medium, $"{ToPrettyString(body):target} suffered {type.ID} to {ToPrettyString(bone):organ}");

        if (silent)
            return true;

        if (bones == null || Timing.CurTime >= bones.NextBreakFeedback)
        {
            if (bones != null)
                bones.NextBreakFeedback = Timing.CurTime + bones.FeedbackCooldown;

            _audio.PlayPvs(BreakSound, body);
            _popup.PopupEntity(Loc.GetString("adt-fracture-self", ("limb", bone.Owner)), body, body, PopupType.LargeCaution);
            _popup.PopupEntity(Loc.GetString("adt-fracture-others", ("target", body)), body, Filter.PvsExcept(body), true, PopupType.MediumCaution);
            TryScream(body);
        }

        if (bones != null && _random.Prob(bones.ReleaseRestraintsChance) && _cuffable.TryGetLastCuff(body, out var cuff))
            _cuffable.Uncuff(body, null, cuff.Value);

        return true;
    }

    public bool Downgrade(EntityUid body, Entity<ADTBoneComponent> bone, ProtoId<ADTFractureTypePrototype> into)
    {
        if (bone.Comp.Fracture == null)
            return false;

        SetFracture(bone, into);
        Refresh(body);
        return true;
    }

    private void Relock(EntityUid body)
    {
        if (_relocking || !TryComp<ADTFracturedComponent>(body, out var fractured) || fractured.LockedDamage <= 0f)
            return;

        if (!TryComp<DamageableComponent>(body, out var damageable))
            return;

        var missing = fractured.LockedDamage - GetBodyBrute(body);
        if (missing <= 0.01f)
            return;

        _relocking = true;
        _damageable.TryChangeDamage((body, damageable), new DamageSpecifier(Proto.Index(Blunt), FixedPoint2.New(missing)), ignoreResistances: true, interruptsDoAfters: false);
        _relocking = false;
    }

    public ADTFractureTypePrototype? PickType(float damage)
    {
        ADTFractureTypePrototype? best = null;
        foreach (var type in Proto.EnumeratePrototypes<ADTFractureTypePrototype>())
        {
            if (type.MinDamage > damage)
                continue;

            if (best == null || type.Power > best.Power)
                best = type;
        }

        return best;
    }

    public ProtoId<OrganCategoryPrototype>? PickZone(Dictionary<ProtoId<OrganCategoryPrototype>, float> weights)
    {
        var total = 0f;
        foreach (var weight in weights.Values)
        {
            total += weight;
        }

        if (total <= 0f)
            return null;

        var roll = _random.NextFloat(total);
        foreach (var (zone, weight) in weights)
        {
            roll -= weight;
            if (roll <= 0f)
                return zone;
        }

        return weights.Keys.Last();
    }

    private static DamageSpecifier GetBruteSpecifier(DamageSpecifier damage)
    {
        var brute = new DamageSpecifier();
        foreach (var type in new[] { Blunt, Slash, Piercing })
        {
            if (damage.DamageDict.TryGetValue(type, out var value) && value > FixedPoint2.Zero)
                brute.DamageDict[type] = value;
        }

        return brute;
    }

    private float GetBodyBrute(EntityUid body)
    {
        if (!TryComp<DamageableComponent>(body, out var damageable))
            return 0f;

        return GetBruteSpecifier(_damageable.GetAllDamage((body, damageable))).GetTotal().Float();
    }

    public bool CanFeelPain(EntityUid body)
    {
        if (_mobState.IsIncapacitated(body) || HasComp<SleepingComponent>(body))
            return false;

        return !HasComp<PainNumbnessStatusEffectComponent>(body) && !_status.HasEffectComp<PainNumbnessStatusEffectComponent>(body);
    }

    public void TryScream(EntityUid body)
    {
        if (!CanFeelPain(body))
            return;

        if (TryComp<ADTBonesComponent>(body, out var bones))
        {
            if (Timing.CurTime < bones.NextScream)
                return;

            bones.NextScream = Timing.CurTime + bones.FeedbackCooldown;
        }

        _chat.TryEmoteWithChat(body, ScreamEmote, ChatTransmitRange.Normal, ignoreActionBlocker: true);
    }
}
