using Content.Server.ADT.Parallax;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Parallax;
using Content.Shared.ADT.Lavaland;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Map.Components;

namespace Content.Server.ADT.NPC;

public sealed class ADTBiomeNPCSleepSystem : EntitySystem
{
    [Dependency] private readonly BiomeSystem _biome = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    private readonly HashSet<Entity<ActiveNPCComponent>> _active = new();
    private readonly HashSet<Entity<ADTChunkSleepingNPCComponent>> _sleeping = new();
    private readonly List<EntityUid> _woken = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLavalandMapComponent, ADTBiomeChunkLoadedEvent>(OnChunkLoaded);
        SubscribeLocalEvent<ADTLavalandMapComponent, ADTBiomeChunkUnloadedEvent>(OnChunkUnloaded);
        SubscribeLocalEvent<ActiveNPCComponent, ComponentStartup>(OnNPCWoken);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_woken.Count == 0)
            return;

        foreach (var uid in _woken)
        {
            if (TerminatingOrDeleted(uid) || !HasComp<ActiveNPCComponent>(uid))
                continue;

            if (!IsInUnloadedChunk(uid))
                continue;

            Sleep(uid);
        }

        _woken.Clear();
    }

    private void OnNPCWoken(Entity<ActiveNPCComponent> ent, ref ComponentStartup args)
    {
        if (IsInUnloadedChunk(ent))
            _woken.Add(ent);
    }

    private void OnChunkUnloaded(Entity<ADTLavalandMapComponent> ent, ref ADTBiomeChunkUnloadedEvent args)
    {
        _active.Clear();
        _lookup.GetLocalEntitiesIntersecting(ent.Owner, args.Bounds, _active, LookupFlags.Dynamic);

        foreach (var npc in _active)
        {
            Sleep(npc);
        }
    }

    private void OnChunkLoaded(Entity<ADTLavalandMapComponent> ent, ref ADTBiomeChunkLoadedEvent args)
    {
        _sleeping.Clear();
        _lookup.GetLocalEntitiesIntersecting(ent.Owner, args.Bounds, _sleeping, LookupFlags.Dynamic);

        foreach (var npc in _sleeping)
        {
            RemComp<ADTChunkSleepingNPCComponent>(npc);

            if (_mobState.IsIncapacitated(npc))
                continue;

            if (TryComp<MindContainerComponent>(npc, out var mind) && mind.HasMind)
                continue;

            if (TryComp<HTNComponent>(npc, out var htn))
                _npc.WakeNPC(npc, htn);
        }
    }

    private void Sleep(EntityUid uid)
    {
        _npc.SleepNPC(uid);
        EnsureComp<ADTChunkSleepingNPCComponent>(uid);
    }

    private bool IsInUnloadedChunk(EntityUid uid)
    {
        var xform = Transform(uid);

        if (xform.GridUid is not { } gridUid || gridUid != xform.MapUid)
            return false;

        if (!HasComp<ADTLavalandMapComponent>(gridUid))
            return false;

        if (!TryComp<BiomeComponent>(gridUid, out var biome) || !TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        return !_biome.IsTileChunkLoaded(biome, tile);
    }
}
