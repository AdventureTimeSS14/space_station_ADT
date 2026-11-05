using Content.Server.ADT.Explosion;
using Content.Shared.ADT.Explosion;
using Content.Server.Administration.Logs;
using Content.Shared.ADT.Mining;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.Explosion;
using Content.Shared.Interaction;
using Content.Shared.Mining.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Mining;

public sealed class ADTGibtoniteSystem : SharedADTGibtoniteSystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDestructibleSystem _destructible = default!;
    [Dependency] private ADTBlastSystem _explosion = default!;
    [Dependency] private ADTMineralSystem _mineral = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly ProtoId<TagPrototype> PickaxeTag = "Pickaxe";
    private static readonly ProtoId<TagPrototype> PlasticTag = "Plastic";
    private static readonly ProtoId<ToolQualityPrototype> SlicingQuality = "Slicing";
    private static readonly ProtoId<ToolQualityPrototype> PulsingQuality = "Pulsing";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTGibtoniteDepositComponent, MapInitEvent>(OnDepositMapInit);
        SubscribeLocalEvent<ADTGibtoniteDepositComponent, InteractUsingEvent>(OnDepositInteractUsing);
        SubscribeLocalEvent<ADTGibtoniteDepositComponent, BeforeExplodeEvent>(OnDepositBeforeExplode);

        SubscribeLocalEvent<ADTGibtoniteOreComponent, MapInitEvent>(OnOreMapInit);
        SubscribeLocalEvent<ADTGibtoniteOreComponent, InteractUsingEvent>(OnOreInteractUsing);
        SubscribeLocalEvent<ADTGibtoniteOreComponent, AttackedEvent>(OnOreAttacked);
        SubscribeLocalEvent<ADTGibtoniteOreComponent, ProjectileHitAttemptEvent>(OnOreProjectileHit);
        SubscribeLocalEvent<ADTGibtoniteOreComponent, BeforeExplodeEvent>(OnOreBeforeExplode);
    }

    private readonly List<Entity<ADTGibtoniteDepositComponent>> _toDetonate = new();
    private readonly List<Entity<ADTGibtoniteOreComponent>> _oresToDetonate = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        _toDetonate.Clear();
        var deposits = EntityQueryEnumerator<ADTActiveGibtoniteComponent, ADTGibtoniteDepositComponent>();
        while (deposits.MoveNext(out var uid, out _, out var deposit))
        {
            if (now < deposit.NextTick)
                continue;

            if (deposit.DetTime > 0)
            {
                deposit.DetTime--;
                deposit.NextTick += deposit.TickTime;
                continue;
            }

            _toDetonate.Add((uid, deposit));
        }

        foreach (var deposit in _toDetonate)
        {
            if (deposit.Comp.Stage == ADTGibtoniteStage.Active)
                Detonate(deposit, deposit.Comp.TimerExplosion);
        }

        _oresToDetonate.Clear();
        var ores = EntityQueryEnumerator<ADTPrimedGibtoniteComponent, ADTGibtoniteOreComponent>();
        while (ores.MoveNext(out var uid, out _, out var ore))
        {
            if (now >= ore.DetonateAt)
                _oresToDetonate.Add((uid, ore));
        }

        foreach (var ore in _oresToDetonate)
        {
            if (ore.Comp.Primed && !TerminatingOrDeleted(ore))
                DetonateOre(ore);
        }
    }

    private void OnDepositMapInit(Entity<ADTGibtoniteDepositComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.DetTime = _random.Next(ent.Comp.MinDetTime, ent.Comp.MaxDetTime + 1);
        UpdateDepositVisuals(ent);
    }

    private void OnDepositInteractUsing(Entity<ADTGibtoniteDepositComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || ent.Comp.Stage != ADTGibtoniteStage.Active)
            return;

        if (!HasComp<MiningScannerComponent>(args.Used) && !HasComp<ADTMagmiteScannerComponent>(args.Used))
            return;

        args.Handled = true;
        Defuse(ent);
    }

    private void OnDepositBeforeExplode(Entity<ADTGibtoniteDepositComponent> ent, ref BeforeExplodeEvent args)
    {
        if (HasComp<ADTMineralComponent>(ent))
            return;

        Drilled(ent, null, true);
    }

    protected override void Drilled(Entity<ADTGibtoniteDepositComponent> ent, EntityUid? user, bool byExplosion)
    {
        if (user != null && HasComp<ADTDefuseGibtoniteOnHitComponent>(user) &&
            ent.Comp.Stage is ADTGibtoniteStage.Unstruck or ADTGibtoniteStage.Active)
        {
            Defuse(ent);
            return;
        }

        switch (ent.Comp.Stage)
        {
            case ADTGibtoniteStage.Unstruck:
                Activate(ent, user, byExplosion);
                break;
            case ADTGibtoniteStage.Active:
                Detonate(ent, ent.Comp.StruckExplosion);
                break;
            case ADTGibtoniteStage.Stable:
                Extract(ent, user);
                break;
            default:
                DestroyDeposit(ent, user);
                break;
        }
    }

    private void Activate(Entity<ADTGibtoniteDepositComponent> ent, EntityUid? user, bool byExplosion)
    {
        ent.Comp.Stage = ADTGibtoniteStage.Active;
        ent.Comp.NextTick = _timing.CurTime;
        EnsureComp<ADTActiveGibtoniteComponent>(ent);
        UpdateDepositVisuals(ent);

        _audio.PlayPvs(ent.Comp.ActivateSound, ent);
        _popup.PopupEntity(Loc.GetString("adt-gibtonite-deposit-active"), ent, PopupType.LargeCaution);

        if (byExplosion || user == null)
            _adminLog.Add(LogType.Explosion, LogImpact.High, $"An explosion has triggered a gibtonite deposit reaction at {ToPrettyString(ent):entity}");
        else
            _adminLog.Add(LogType.Explosion, LogImpact.High, $"{ToPrettyString(user.Value):user} has triggered a gibtonite deposit reaction at {ToPrettyString(ent):entity}");
    }

    private void Defuse(Entity<ADTGibtoniteDepositComponent> ent)
    {
        if (ent.Comp.Stage is not (ADTGibtoniteStage.Active or ADTGibtoniteStage.Unstruck))
            return;

        var wasActive = ent.Comp.Stage == ADTGibtoniteStage.Active;
        ent.Comp.Stage = ADTGibtoniteStage.Stable;
        ent.Comp.DetTime = Math.Max(0, ent.Comp.DetTime);
        RemComp<ADTActiveGibtoniteComponent>(ent);
        UpdateDepositVisuals(ent);

        if (wasActive)
            _popup.PopupEntity(Loc.GetString("adt-gibtonite-deposit-defused", ("left", ent.Comp.DetTime)), ent);
    }

    private void Detonate(Entity<ADTGibtoniteDepositComponent> ent, ADTBlastRadii explosion)
    {
        ent.Comp.Stage = ADTGibtoniteStage.Detonated;
        RemComp<ADTActiveGibtoniteComponent>(ent);

        var coords = _transform.GetMapCoordinates(ent);
        DestroyDeposit(ent, null);
        _explosion.Explode(coords, explosion, ent);
    }

    private void Extract(Entity<ADTGibtoniteDepositComponent> ent, EntityUid? user)
    {
        var quality = ADTGibtoniteQuality.Low;
        if (ent.Comp.DetTime <= ent.Comp.HighQualityDetTime)
            quality = ADTGibtoniteQuality.High;
        else if (ent.Comp.DetTime <= ent.Comp.MediumQualityDetTime)
            quality = ADTGibtoniteQuality.Medium;

        var ore = Spawn(ent.Comp.Ore, Transform(ent).Coordinates);
        if (TryComp<ADTGibtoniteOreComponent>(ore, out var oreComp))
        {
            oreComp.Quality = quality;
            UpdateOreVisuals((ore, oreComp));
        }

        ent.Comp.Stage = ADTGibtoniteStage.Detonated;
        DestroyDeposit(ent, user);
    }

    private void DestroyDeposit(Entity<ADTGibtoniteDepositComponent> ent, EntityUid? user)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (TryComp<ADTMineralComponent>(ent, out var mineral))
        {
            _mineral.Destroy((ent, mineral), user);
            return;
        }

        _destructible.DestroyEntity(ent);
    }

    private void UpdateDepositVisuals(Entity<ADTGibtoniteDepositComponent> ent)
    {
        _appearance.SetData(ent, ADTGibtoniteVisuals.Stage, ent.Comp.Stage);
    }

    private void OnOreMapInit(Entity<ADTGibtoniteOreComponent> ent, ref MapInitEvent args)
    {
        UpdateOreVisuals(ent);
    }

    private void OnOreInteractUsing(Entity<ADTGibtoniteOreComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Primed &&
            (HasComp<MiningScannerComponent>(args.Used) || HasComp<ADTMagmiteScannerComponent>(args.Used) || _tool.HasQuality(args.Used, PulsingQuality)))
        {
            args.Handled = true;
            DefuseOre(ent, args.User);
            return;
        }

        if (_tool.HasQuality(args.Used, SlicingQuality) && _tag.HasTag(args.Used, PlasticTag))
        {
            args.Handled = true;
            SliceOre(ent);
            return;
        }

        if (!CanPrime(args.Used))
            return;

        args.Handled = true;
        PrimeOre(ent, args.User);
    }

    private void OnOreAttacked(Entity<ADTGibtoniteOreComponent> ent, ref AttackedEvent args)
    {
        if (CanPrime(args.Used))
            PrimeOre(ent, args.User);
    }

    private void OnOreProjectileHit(Entity<ADTGibtoniteOreComponent> ent, ref ProjectileHitAttemptEvent args)
    {
        PrimeOre(ent, args.Shooter);
    }

    private void OnOreBeforeExplode(Entity<ADTGibtoniteOreComponent> ent, ref BeforeExplodeEvent args)
    {
        PrimeOre(ent, null);
    }

    private bool CanPrime(EntityUid used)
    {
        if (_tag.HasTag(used, PickaxeTag) || HasComp<ADTMiningToolComponent>(used))
            return true;

        return TryComp<MeleeWeaponComponent>(used, out var melee) && melee.Damage.GetTotal() >= 10;
    }

    public void PrimeOre(Entity<ADTGibtoniteOreComponent> ent, EntityUid? user)
    {
        if (ent.Comp.Primed || TerminatingOrDeleted(ent))
            return;

        ent.Comp.Primed = true;
        ent.Comp.DetonateAt = _timing.CurTime + ent.Comp.DetonationDelay;
        EnsureComp<ADTPrimedGibtoniteComponent>(ent);
        UpdateOreVisuals(ent);

        _audio.PlayPvs(ent.Comp.PrimeSound, ent);
        _popup.PopupEntity(Loc.GetString("adt-gibtonite-ore-primed"), ent, PopupType.LargeCaution);

        if (user != null)
            _adminLog.Add(LogType.Explosion, LogImpact.High, $"{ToPrettyString(user.Value):user} has primed {ToPrettyString(ent):entity} for detonation");
        else
            _adminLog.Add(LogType.Explosion, LogImpact.High, $"{ToPrettyString(ent):entity} was primed for detonation");
    }

    private void DefuseOre(Entity<ADTGibtoniteOreComponent> ent, EntityUid user)
    {
        ent.Comp.Primed = false;
        ent.Comp.Quality = ADTGibtoniteQuality.Low;
        RemComp<ADTPrimedGibtoniteComponent>(ent);
        UpdateOreVisuals(ent);

        _popup.PopupEntity(Loc.GetString("adt-gibtonite-ore-defused"), ent, user);
    }

    private void SliceOre(Entity<ADTGibtoniteOreComponent> ent)
    {
        var coords = _transform.GetMapCoordinates(ent);
        for (var i = 0; i < ent.Comp.ShardCount; i++)
        {
            Spawn(ent.Comp.Shard, coords);
        }

        QueueDel(ent);
    }

    private void DetonateOre(Entity<ADTGibtoniteOreComponent> ent)
    {
        RemComp<ADTPrimedGibtoniteComponent>(ent);

        var explosion = ent.Comp.Quality switch
        {
            ADTGibtoniteQuality.High => ent.Comp.HighExplosion,
            ADTGibtoniteQuality.Medium => ent.Comp.MediumExplosion,
            _ => ent.Comp.LowExplosion,
        };

        var coords = _transform.GetMapCoordinates(ent);
        QueueDel(ent);
        _explosion.Explode(coords, explosion, ent);
    }

    private void UpdateOreVisuals(Entity<ADTGibtoniteOreComponent> ent)
    {
        _appearance.SetData(ent, ADTGibtoniteVisuals.Quality, ent.Comp.Quality);
        _appearance.SetData(ent, ADTGibtoniteVisuals.Primed, ent.Comp.Primed);
    }
}
