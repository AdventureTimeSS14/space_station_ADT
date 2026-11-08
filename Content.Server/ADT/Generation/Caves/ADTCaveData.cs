namespace Content.Server.ADT.Generation.Caves;

public sealed class ADTCaveData
{
    public readonly Vector2i Origin;
    public readonly int Width;
    public readonly int Height;
    public readonly byte[] Types;
    public readonly byte[] Ores;
    public readonly byte[] Amounts;

    public ADTCaveData(Vector2i origin, int width, int height)
    {
        Origin = origin;
        Width = width;
        Height = height;
        Types = new byte[width * height];
        Ores = new byte[width * height];
        Amounts = new byte[width * height];
    }

    public bool TryGetIndex(Vector2i tile, out int index)
    {
        var x = tile.X - Origin.X;
        var y = tile.Y - Origin.Y;

        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            index = -1;
            return false;
        }

        index = y * Width + x;
        return true;
    }
}
