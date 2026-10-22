namespace Content.Server.ADT.Generation.Caves;

// LavalandGenerator.dm port (generate_terrain + nodes hard/double) and /turf/simulated/mineral/random/Initialize + Spread.
public sealed class ADTCaveGenerator
{
    private static readonly (int X, int Y)[] Cardinals =
    {
        (0, 1),
        (0, -1),
        (1, 0),
        (-1, 0),
    };

    private readonly Random _random;
    private readonly ADTCaveData _data;
    private readonly WallSettings[] _walls;
    private readonly float[] _spread;
    private readonly float _extraChance;
    private readonly (int Ore, float Weight)[] _extraOres;
    private readonly int _minNodes;
    private readonly int _maxNodes;
    private readonly int _nodeMinSize;
    private readonly int _nodeMaxSize;
    private readonly NoiseSettings _hard;
    private readonly NoiseSettings _double;
    private readonly float _closedChance;
    private readonly int _iterations;
    private readonly int _birthLimit;
    private readonly int _deathLimit;

    public readonly List<string> OreNames;

    public ADTCaveGenerator(ADTLavalandCavesComponent comp, int seed, Vector2i origin, int size)
    {
        _random = new Random(seed);
        _data = new ADTCaveData(origin, size, size);

        OreNames = new List<string>();
        foreach (var wall in comp.Walls)
        {
            foreach (var ore in wall.Ores.Keys)
            {
                if (!OreNames.Contains(ore))
                    OreNames.Add(ore);
            }
        }

        foreach (var ore in comp.ExtraOres.Keys)
        {
            if (!OreNames.Contains(ore))
                OreNames.Add(ore);
        }

        _walls = new WallSettings[comp.Walls.Count];
        for (var i = 0; i < comp.Walls.Count; i++)
        {
            var wall = comp.Walls[i];
            _walls[i] = new WallSettings(wall.MineralChance, ToWeights(wall.Ores));
        }

        _spread = new float[OreNames.Count + 1];
        foreach (var (ore, chance) in comp.Spread)
        {
            var index = OreNames.IndexOf(ore);
            if (index >= 0)
                _spread[index + 1] = chance;
        }

        _extraChance = comp.ExtraChance;
        _extraOres = ToWeights(comp.ExtraOres);
        _minNodes = comp.MinNodes;
        _maxNodes = comp.MaxNodes;
        _nodeMinSize = comp.NodeMinSize;
        _nodeMaxSize = comp.NodeMaxSize;
        _hard = new NoiseSettings(comp.HardNoise.Chance, comp.HardNoise.Iterations, comp.HardNoise.BirthLimit, comp.HardNoise.DeathLimit);
        _double = new NoiseSettings(comp.DoubleNoise.Chance, comp.DoubleNoise.Iterations, comp.DoubleNoise.BirthLimit, comp.DoubleNoise.DeathLimit);
        _closedChance = comp.ClosedChance;
        _iterations = comp.SmoothingIterations;
        _birthLimit = comp.BirthLimit;
        _deathLimit = comp.DeathLimit;
    }

    public ADTCaveData Generate()
    {
        GenerateTerrain();
        GenerateNodes();
        ResolveMinerals();
        ResolveExtraMinerals();
        return _data;
    }

    private (int Ore, float Weight)[] ToWeights(Dictionary<string, float> ores)
    {
        var result = new (int Ore, float Weight)[ores.Count];
        var i = 0;
        foreach (var (ore, weight) in ores)
        {
            result[i++] = (OreNames.IndexOf(ore) + 1, weight);
        }

        return result;
    }

    private void GenerateTerrain()
    {
        var width = _data.Width;
        var height = _data.Height;
        var noise = ADTCellularNoise.Generate(_random, _closedChance, _iterations, _birthLimit, _deathLimit, width, height);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                if (noise[width * y + x])
                    _data.Types[y * width + x] = 1;
            }
        }
    }

    private void GenerateNodes()
    {
        var width = _data.Width;
        var height = _data.Height;
        var count = _random.Next(_minNodes, _maxNodes + 1);
        var taken = new HashSet<int>();

        for (var node = 0; node < count; node++)
        {
            int picked;
            do
            {
                picked = _random.Next(width * height);
            }
            while (!taken.Add(picked));

            var px = picked % width;
            var py = picked / width;
            var sizeX = _random.Next(_nodeMinSize, _nodeMaxSize + 1);
            var sizeY = _random.Next(_nodeMinSize, _nodeMaxSize + 1);

            var hard = ADTCellularNoise.Generate(_random, _hard.Chance, _hard.Iterations, _hard.BirthLimit, _hard.DeathLimit, sizeX + 1, sizeY + 1);
            var hardDouble = ADTCellularNoise.Generate(_random, _double.Chance, _double.Iterations, _double.BirthLimit, _double.DeathLimit, sizeX + 1, sizeY + 1);

            var halfX = sizeX / 2;
            var halfY = sizeY / 2;
            var minX = Math.Max(0, px - halfX);
            var minY = Math.Max(0, py - halfY);
            var maxX = Math.Min(width - 1, px + halfX);
            var maxY = Math.Min(height - 1, py + halfY);

            var index = 0;
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++, index++)
                {
                    var cell = y * width + x;
                    if (_data.Types[cell] != 1 || index >= hard.Length)
                        continue;

                    var hardened = (hard[index] ? 1 : 0) + (hardDouble[index] ? 1 : 0);
                    if (hardened > 0)
                        _data.Types[cell] = (byte) (1 + hardened);
                }
            }
        }
    }

    private void ResolveMinerals()
    {
        var total = _data.Width * _data.Height;
        for (var cell = 0; cell < total; cell++)
        {
            var type = _data.Types[cell];
            if (type == 0 || _data.Ores[cell] != 0 || type > _walls.Length)
                continue;

            var wall = _walls[type - 1];
            if (!Prob(wall.MineralChance))
                continue;

            SeedOre(cell, type, PickWeight(wall.Ores));
        }
    }

    private void ResolveExtraMinerals()
    {
        if (_extraChance <= 0f || _extraOres.Length == 0)
            return;

        var total = _data.Width * _data.Height;
        for (var cell = 0; cell < total; cell++)
        {
            var type = _data.Types[cell];
            if (type == 0 || _data.Ores[cell] != 0)
                continue;

            if (!Prob(_extraChance))
                continue;

            SeedOre(cell, type, PickWeight(_extraOres));
        }
    }

    private void SeedOre(int cell, byte type, int ore)
    {
        if (ore <= 0)
            return;

        _data.Ores[cell] = (byte) ore;
        Spread(cell);
        _data.Amounts[cell] = (byte) (_random.Next(1, 3) + Math.Max(0, type - 1));
    }

    private void Spread(int cell)
    {
        var ore = _data.Ores[cell];
        var chance = _spread[ore];
        if (chance <= 0f)
            return;

        var x = cell % _data.Width;
        var y = cell / _data.Width;

        foreach (var (dx, dy) in Cardinals)
        {
            if (!Prob(chance))
                continue;

            var nx = x + dx;
            var ny = y + dy;
            if (nx < 0 || ny < 0 || nx >= _data.Width || ny >= _data.Height)
                continue;

            var neighbour = ny * _data.Width + nx;
            if (_data.Types[neighbour] == 0 || _data.Ores[neighbour] != 0)
                continue;

            _data.Types[neighbour] = _data.Types[cell];
            _data.Ores[neighbour] = ore;
            _data.Amounts[neighbour] = 1;
            Spread(neighbour);
        }
    }

    private int PickWeight((int Ore, float Weight)[] weights)
    {
        var sum = 0f;
        foreach (var (_, weight) in weights)
        {
            sum += weight;
        }

        if (sum <= 0f)
            return 0;

        var roll = _random.NextDouble() * sum;
        foreach (var (ore, weight) in weights)
        {
            roll -= weight;
            if (roll <= 0)
                return ore;
        }

        return weights[^1].Ore;
    }

    private bool Prob(float percent)
    {
        return _random.NextDouble() * 100.0 < percent;
    }

    private readonly record struct NoiseSettings(float Chance, int Iterations, int BirthLimit, int DeathLimit);

    private readonly record struct WallSettings(float MineralChance, (int Ore, float Weight)[] Ores);
}
