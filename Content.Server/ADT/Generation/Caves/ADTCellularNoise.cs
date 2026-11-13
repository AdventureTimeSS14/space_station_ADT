namespace Content.Server.ADT.Generation.Caves;

// rustg_cnoise_generate tgstation/rust-g (src/cellularnoise.rs) port.
public static class ADTCellularNoise
{
    public static bool[] Generate(Random random, float percentage, int iterations, int birthLimit, int deathLimit, int width, int height)
    {
        var fullWidth = width + 3;
        var fullHeight = height + 3;
        var grid = new bool[fullWidth * fullHeight];
        var next = new bool[fullWidth * fullHeight];
        var chance = Math.Clamp(percentage / 100.0, 0.0, 1.0);

        for (var x = 1; x < fullWidth - 1; x++)
        {
            for (var y = 1; y < fullHeight - 1; y++)
            {
                grid[x * fullHeight + y] = random.NextDouble() < chance;
            }
        }

        for (var i = 0; i < iterations; i++)
        {
            for (var x = 1; x < fullWidth - 1; x++)
            {
                for (var y = 1; y < fullHeight - 1; y++)
                {
                    var index = x * fullHeight + y;
                    var count = 0;

                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var column = (x + dx) * fullHeight + y;
                        for (var dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0)
                                continue;

                            if (grid[column + dy])
                                count++;
                        }
                    }

                    if (grid[index])
                        next[index] = count >= deathLimit;
                    else
                        next[index] = count > birthLimit;
                }
            }

            (grid, next) = (next, grid);
        }

        var result = new bool[width * height];
        for (var x = 1; x <= width; x++)
        {
            for (var y = 1; y <= height; y++)
            {
                result[(x - 1) * height + (y - 1)] = grid[x * fullHeight + y];
            }
        }

        return result;
    }
}
