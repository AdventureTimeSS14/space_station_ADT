using System.Linq;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.ADT.Lavaland.Helios;
using Content.Shared.DeviceLinking;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Lavaland.Helios;

public sealed class ADTHeliosPuzzleSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DeviceLinkSystem _deviceLink = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private const int North = 1;
    private const int East = 2;
    private const int South = 4;
    private const int West = 8;

    private static readonly (int Bit, Vector2i Offset, int Opposite)[] Sides =
    {
        (North, new Vector2i(0, 1), South),
        (East, new Vector2i(1, 0), West),
        (South, new Vector2i(0, -1), North),
        (West, new Vector2i(-1, 0), East),
    };

    private static readonly TimeSpan PlateCheckInterval = TimeSpan.FromSeconds(0.5);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLightsOutPuzzleComponent, MapInitEvent>(OnLightsInit);
        SubscribeLocalEvent<ADTLightsOutPanelComponent, InteractHandEvent>(OnPanelInteract);

        SubscribeLocalEvent<ADTConduitPuzzleComponent, MapInitEvent>(OnConduitInit);
        SubscribeLocalEvent<ADTConduitPieceComponent, InteractHandEvent>(OnPieceInteract);
    }

    private void OnLightsInit(Entity<ADTLightsOutPuzzleComponent> ent, ref MapInitEvent args)
    {
        if (!TryGetOrigin(ent, out var grid, out var origin))
            return;

        var comp = ent.Comp;
        var count = comp.Width * comp.Height;
        comp.Panels.Clear();
        comp.Lit.Clear();

        for (var i = 0; i < count; i++)
        {
            var tile = origin + new Vector2i(i % comp.Width, i / comp.Width);
            var panel = Spawn(comp.PanelProto, _map.GridTileToLocal(grid, grid, tile));
            var panelComp = EnsureComp<ADTLightsOutPanelComponent>(panel);
            panelComp.Source = ent;
            panelComp.Index = i;
            comp.Panels.Add(panel);
            comp.Lit.Add(true);
        }

        var cells = Enumerable.Range(0, count).ToList();
        _random.Shuffle(cells);
        comp.ScramblePresses = cells.Take(Math.Clamp(comp.Scramble, 1, count)).ToList();
        foreach (var index in comp.ScramblePresses)
            Toggle(comp, index);

        UpdatePanels(comp);
    }

    private void OnPanelInteract(Entity<ADTLightsOutPanelComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = PressPanel(ent, args.User);
    }

    public bool PressPanel(Entity<ADTLightsOutPanelComponent> ent, EntityUid? user = null)
    {
        if (!TryComp<ADTLightsOutPuzzleComponent>(ent.Comp.Source, out var puzzle) || puzzle.Finished)
            return false;

        Toggle(puzzle, ent.Comp.Index);
        UpdatePanels(puzzle);
        _audio.PlayPvs(puzzle.ToggleSound, ent);

        if (puzzle.Lit.All(l => l))
        {
            puzzle.Finished = true;
            Solved(ent.Comp.Source, puzzle.SolvedPort, puzzle.SolvedMessage, puzzle.SolveSound);
        }

        return true;
    }

    private static void Toggle(ADTLightsOutPuzzleComponent comp, int index)
    {
        var x = index % comp.Width;
        var y = index / comp.Width;
        Flip(comp, x, y);
        Flip(comp, x + 1, y);
        Flip(comp, x - 1, y);
        Flip(comp, x, y + 1);
        Flip(comp, x, y - 1);
    }

    private static void Flip(ADTLightsOutPuzzleComponent comp, int x, int y)
    {
        if (x < 0 || y < 0 || x >= comp.Width || y >= comp.Height)
            return;

        var i = y * comp.Width + x;
        comp.Lit[i] = !comp.Lit[i];
    }

    private void UpdatePanels(ADTLightsOutPuzzleComponent comp)
    {
        for (var i = 0; i < comp.Panels.Count; i++)
        {
            if (!TerminatingOrDeleted(comp.Panels[i]))
                _appearance.SetData(comp.Panels[i], ADTHeliosPuzzleVisuals.Lit, comp.Lit[i]);
        }
    }

    private void OnConduitInit(Entity<ADTConduitPuzzleComponent> ent, ref MapInitEvent args)
    {
        if (!TryGetOrigin(ent, out var grid, out var origin))
            return;

        var comp = ent.Comp;
        var w = comp.Width;
        var h = comp.Height;
        var entry = new Vector2i(0, Math.Clamp(comp.EntryRow, 0, h - 1));
        var exit = new Vector2i(w - 1, Math.Clamp(comp.ExitRow, 0, h - 1));

        var path = RandomPath(w, h, entry, exit);
        var required = new int[w * h];
        for (var i = 0; i < path.Count; i++)
        {
            var mask = 0;
            mask |= i == 0 ? West : DirBit(path[i - 1] - path[i]);
            mask |= i == path.Count - 1 ? East : DirBit(path[i + 1] - path[i]);
            required[path[i].Y * w + path[i].X] = mask;
        }

        comp.Pieces.Clear();
        comp.Solution.Clear();

        for (var i = 0; i < w * h; i++)
        {
            var cell = new Vector2i(i % w, i / w);
            var need = required[i];
            string proto;
            var target = -1;

            if (need != 0)
            {
                proto = IsStraight(need) ? comp.StraightProto : comp.BendProto;
            }
            else
            {
                var roll = _random.Next(10);
                proto = roll < 4 ? comp.StraightProto : roll < 8 ? comp.BendProto : comp.TeeProto;
            }

            var piece = Spawn(proto, _map.GridTileToLocal(grid, grid, origin + cell));
            var pieceComp = EnsureComp<ADTConduitPieceComponent>(piece);
            pieceComp.Source = ent;
            pieceComp.Index = i;

            if (need != 0)
            {
                for (var r = 0; r < 4; r++)
                {
                    if (RotateMask(pieceComp.BaseMask, r) == need)
                    {
                        target = r;
                        break;
                    }
                }
            }

            pieceComp.Rotation = _random.Next(4);
            ApplyRotation((piece, pieceComp));
            comp.Pieces.Add(piece);
            comp.Solution.Add(target);
        }

        if (IsPowered(comp, out _))
        {
            var first = comp.Pieces[entry.Y * w + entry.X];
            var firstComp = Comp<ADTConduitPieceComponent>(first);
            firstComp.Rotation = (firstComp.Rotation + 1) % 4;
            ApplyRotation((first, firstComp));
        }

        UpdateConduit(comp);
    }

    private void OnPieceInteract(Entity<ADTConduitPieceComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = RotatePiece(ent);
    }

    public bool RotatePiece(Entity<ADTConduitPieceComponent> ent)
    {
        if (!TryComp<ADTConduitPuzzleComponent>(ent.Comp.Source, out var puzzle) || puzzle.Finished)
            return false;

        ent.Comp.Rotation = (ent.Comp.Rotation + 1) % 4;
        ApplyRotation(ent);
        _audio.PlayPvs(puzzle.RotateSound, ent);

        if (UpdateConduit(puzzle))
        {
            puzzle.Finished = true;
            Solved(ent.Comp.Source, puzzle.SolvedPort, puzzle.SolvedMessage, puzzle.SolveSound);
        }

        return true;
    }

    private void ApplyRotation(Entity<ADTConduitPieceComponent> ent)
    {
        _transform.SetLocalRotation(ent, Angle.FromDegrees(-90 * ent.Comp.Rotation));
    }

    private bool UpdateConduit(ADTConduitPuzzleComponent comp)
    {
        var solved = IsPowered(comp, out var powered);
        for (var i = 0; i < comp.Pieces.Count; i++)
        {
            if (!TerminatingOrDeleted(comp.Pieces[i]))
                _appearance.SetData(comp.Pieces[i], ADTHeliosPuzzleVisuals.Lit, powered.Contains(i));
        }

        return solved;
    }

    private bool IsPowered(ADTConduitPuzzleComponent comp, out HashSet<int> powered)
    {
        var w = comp.Width;
        var h = comp.Height;
        powered = new HashSet<int>();
        var masks = new int[comp.Pieces.Count];
        for (var i = 0; i < comp.Pieces.Count; i++)
        {
            masks[i] = TryComp<ADTConduitPieceComponent>(comp.Pieces[i], out var p) ? RotateMask(p.BaseMask, p.Rotation) : 0;
        }

        var entry = Math.Clamp(comp.EntryRow, 0, h - 1) * w;
        if (masks.Length == 0 || (masks[entry] & West) == 0)
            return false;

        var queue = new Queue<int>();
        queue.Enqueue(entry);
        powered.Add(entry);

        while (queue.Count > 0)
        {
            var i = queue.Dequeue();
            var cell = new Vector2i(i % w, i / w);
            foreach (var (bit, offset, opposite) in Sides)
            {
                if ((masks[i] & bit) == 0)
                    continue;

                var next = cell + offset;
                if (next.X < 0 || next.Y < 0 || next.X >= w || next.Y >= h)
                    continue;

                var j = next.Y * w + next.X;
                if ((masks[j] & opposite) == 0 || !powered.Add(j))
                    continue;

                queue.Enqueue(j);
            }
        }

        var exit = Math.Clamp(comp.ExitRow, 0, h - 1) * w + (w - 1);
        return powered.Contains(exit) && (masks[exit] & East) != 0;
    }

    private List<Vector2i> RandomPath(int w, int h, Vector2i from, Vector2i to)
    {
        var visited = new HashSet<Vector2i> { from };
        var path = new List<Vector2i> { from };

        bool Walk(Vector2i cell)
        {
            if (cell == to)
                return true;

            var options = Sides.Select(s => cell + s.Offset)
                .Where(n => n.X >= 0 && n.Y >= 0 && n.X < w && n.Y < h && !visited.Contains(n))
                .ToList();
            _random.Shuffle(options);

            foreach (var next in options)
            {
                visited.Add(next);
                path.Add(next);
                if (Walk(next))
                    return true;

                path.RemoveAt(path.Count - 1);
            }

            return false;
        }

        Walk(from);
        return path;
    }

    private static int DirBit(Vector2i offset)
    {
        foreach (var (bit, off, _) in Sides)
        {
            if (off == offset)
                return bit;
        }

        return 0;
    }

    private static bool IsStraight(int mask) => mask == (North | South) || mask == (East | West);

    public static int RotateMask(int mask, int quarters)
    {
        for (var i = 0; i < quarters % 4; i++)
            mask = ((mask << 1) | (mask >> 3)) & 15;

        return mask;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTGlyphPlatePuzzleComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var puzzle, out var xform))
        {
            if (puzzle.Finished || now < puzzle.NextCheck)
                continue;

            puzzle.NextCheck = now + PlateCheckInterval;
            CheckPlates((uid, puzzle), xform.MapID);
        }
    }

    private void CheckPlates(Entity<ADTGlyphPlatePuzzleComponent> puzzle, MapId map)
    {
        var all = true;
        var any = false;
        var samples = new List<EntityUid>();

        var plates = EntityQueryEnumerator<ADTGlyphPlateComponent, TransformComponent>();
        while (plates.MoveNext(out var plateUid, out var plate, out var xform))
        {
            if (plate.Group != puzzle.Comp.Group || xform.MapID != map)
                continue;

            any = true;
            var matched = false;
            foreach (var other in _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(plateUid, xform), 0.45f, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (TryComp<ADTGlyphSampleComponent>(other, out var sample) && sample.Glyph == plate.Glyph)
                {
                    matched = true;
                    samples.Add(other);
                    break;
                }
            }

            if (matched != plate.Matched)
            {
                plate.Matched = matched;
                _appearance.SetData(plateUid, ADTHeliosPuzzleVisuals.Lit, matched);
                if (matched)
                    _audio.PlayPvs(puzzle.Comp.PlateSound, plateUid);
            }

            all &= matched;
        }

        if (!any || !all)
            return;

        puzzle.Comp.Finished = true;

        foreach (var sample in samples)
        {
            if (TryComp<PhysicsComponent>(sample, out var body))
                _physics.SetBodyType(sample, BodyType.Static, body: body);
        }

        Solved(puzzle, puzzle.Comp.SolvedPort, puzzle.Comp.SolvedMessage, puzzle.Comp.SolveSound);
    }

    private bool TryGetOrigin(EntityUid uid, out Entity<MapGridComponent> grid, out Vector2i origin)
    {
        var xform = Transform(uid);
        if (xform.GridUid is { } gridUid && TryComp<MapGridComponent>(gridUid, out var gridComp))
        {
            grid = (gridUid, gridComp);
            origin = _map.CoordinatesToTile(gridUid, gridComp, xform.Coordinates);
            return true;
        }

        grid = default;
        origin = default;
        return false;
    }

    private void Solved(EntityUid uid, ProtoId<SourcePortPrototype>? port, LocId message, SoundSpecifier? sound)
    {
        if (port is { } p)
            _deviceLink.InvokePort(uid, p);

        _audio.PlayPvs(sound, uid);
        _popup.PopupEntity(Loc.GetString(message), uid, PopupType.Medium);
    }
}
