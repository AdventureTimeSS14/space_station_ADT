using Content.Server.ADT.Generation.Caves;
using Content.Server.Stack;
using Content.Shared.ADT.Mining;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.Explosion;
using Content.Shared.Mining;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Mining;

public sealed class ADTMineralSystem : SharedADTMineralSystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private StackSystem _stack = default!;
    [Dependency] private ADTLavalandCaveSystem _caves = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public static readonly ProtoId<ExplosionPrototype> BlastExplosion = "ADTBlast";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMineralComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTMineralComponent, BeforeExplodeEvent>(OnBeforeExplode);
    }

    private void OnMapInit(Entity<ADTMineralComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.MaxHardness = ent.Comp.Hardness;
        Dirty(ent);

        if (TryComp<ADTMineralOreComponent>(ent, out var mineralOre) && mineralOre.FixedAmount > 0)
        {
            ent.Comp.OreAmount = mineralOre.FixedAmount;
            return;
        }

        if (ent.Comp.OreAmount > 0)
            return;

        if (TryGetCaveAmount(ent, out var amount))
        {
            ent.Comp.OreAmount = amount;
            return;
        }

        ent.Comp.OreAmount = _random.Next(1, 3) + Math.Max(0, ent.Comp.Hardness - 1);
    }

    private bool TryGetCaveAmount(EntityUid uid, out int amount)
    {
        amount = 0;
        var xform = Transform(uid);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        if (!_caves.TryGetWall(gridUid, tile, out var prototype) || prototype != MetaData(uid).EntityPrototype?.ID)
            return false;

        return _caves.TryGetOreAmount(gridUid, tile, out amount);
    }

    private void OnBeforeExplode(Entity<ADTMineralComponent> ent, ref BeforeExplodeEvent args)
    {
        if (args.Id == BlastExplosion.Id)
            return;

        if (!_prototype.TryIndex<ExplosionPrototype>(args.Id, out var explosion))
            return;

        var perIntensity = explosion.DamagePerIntensity.GetTotal().Float();
        if (perIntensity <= 0f)
            return;

        var intensity = args.Damage.GetTotal().Float() / perIntensity;

        var severity = 1;
        if (intensity >= ent.Comp.DevastationIntensity)
            severity = 3;
        else if (intensity >= ent.Comp.HeavyIntensity)
            severity = 2;

        ExplosionAct(ent, severity);
    }

    public void ExplosionAct(Entity<ADTMineralComponent> ent, int severity)
    {
        var power = 0;
        switch (severity)
        {
            case >= 3:
                power = 3;
                break;
            case 2:
                power = _random.Prob(0.9f) ? 2 : 1;
                break;
            default:
                if (_random.Prob(0.75f))
                    power = 2;
                else if (_random.Prob(0.9f))
                    power = 1;
                break;
        }

        Mine(ent.AsNullable(), power, null, true);
    }

    protected override void OnHit(Entity<ADTMineralComponent> ent, EntityUid source, EntityUid? predictedBy)
    {
        var direction = _transform.GetWorldPosition(source) - _transform.GetWorldPosition(ent);
        var filter = Filter.Pvs(ent);
        if (TryComp<ActorComponent>(predictedBy, out var actor))
            filter.RemovePlayer(actor.PlayerSession);

        RaiseNetworkEvent(new ADTMineralHitEvent(GetNetEntity(ent), direction), filter);
    }

    protected override void OnBroken(Entity<ADTMineralComponent> ent, EntityUid? user, bool byExplosion)
    {
        if (!TryComp<ADTMineralOreComponent>(ent, out var mineralOre) || mineralOre.Ore is not { } oreId)
            return;

        if (!_prototype.TryIndex(oreId, out OrePrototype? ore) || ore.OreEntity is not { } oreEntity)
            return;

        var amount = ent.Comp.OreAmount;
        if (byExplosion && !mineralOre.NoExplosionBonus)
            amount += ent.Comp.ExplosionBonus;

        if (amount <= 0)
            return;

        _stack.SpawnMultipleAtPosition(oreEntity, amount, Transform(ent).Coordinates);
    }
}
