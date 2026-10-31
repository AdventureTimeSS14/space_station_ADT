namespace Content.Shared.ADT.Drawing;

public static class CanvasConstants
{
    public const int TileSize = 256;
    public const int MaxSide = 4096;
    public const int MaxTileBytes = 384 * 1024;
    public const int HashLength = 16;
    public const int MinSide = 256;
    public const int SideStep = 64;
    public const int MinPixelSide = 8;
    public const int MaxPixelSide = 128;
    public const int PixelSideStep = 8;
    public const int ChunkSize = 16 * 1024;
    public const int MaxChunks = (MaxTileBytes + ChunkSize - 1) / ChunkSize;
    public const int MaxBlobRequest = 64;
    public const int MaxTiles = MaxSide / TileSize * (MaxSide / TileSize);

    public const int ThumbSide = 256;
    public const int MaxTitleLength = 64;
    public const int MaxDescriptionLength = 256;
    public const int MaxReasonLength = 512;

    public const byte NoAuthor = byte.MaxValue;
    public const int MaxStrokeIds = 64;
    public const int MaxStrokeSamples = 256;
    public const float MaxBrushSize = 1024f;
    public const float CoordScale = 8f;
    public const float SizeScale = 4f;

    public static Vector2i ThumbSize(int width, int height)
    {
        var longest = Math.Max(width, height);
        if (longest <= ThumbSide)
            return new Vector2i(width, height);

        return new Vector2i(
            Math.Max(1, (int) Math.Round(width * (double) ThumbSide / longest)),
            Math.Max(1, (int) Math.Round(height * (double) ThumbSide / longest)));
    }

    public static Vector2i LevelSize(int width, int height, int lod)
    {
        var w = width;
        var h = height;
        for (var i = 0; i < lod; i++)
        {
            w = Math.Max(1, (w + 1) / 2);
            h = Math.Max(1, (h + 1) / 2);
        }

        return new Vector2i(w, h);
    }

    public static int LevelCount(int width, int height)
    {
        var count = 1;
        var w = width;
        var h = height;
        while (Math.Max(w, h) > TileSize)
        {
            w = Math.Max(1, (w + 1) / 2);
            h = Math.Max(1, (h + 1) / 2);
            count++;
        }

        return count;
    }

    public static bool IsValidSize(Vector2i size)
    {
        return IsValidSide(size.X) && IsValidSide(size.Y) || IsPixelSize(size);
    }

    public static bool IsPixelSize(Vector2i size)
    {
        return IsValidPixelSide(size.X) && IsValidPixelSide(size.Y);
    }

    private static bool IsValidSide(int side)
    {
        return side >= MinSide && side <= MaxSide && side % SideStep == 0;
    }

    private static bool IsValidPixelSide(int side)
    {
        return side >= MinPixelSide && side <= MaxPixelSide && side % PixelSideStep == 0;
    }

    public static int TileCount(int side)
    {
        return (side + TileSize - 1) / TileSize;
    }

    public static int TileExtent(int side, int tile)
    {
        return Math.Min(TileSize, side - tile * TileSize);
    }
}
