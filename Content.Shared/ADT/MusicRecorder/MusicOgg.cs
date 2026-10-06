namespace Content.Shared.ADT.MusicRecorder;

public static class MusicOgg
{
    public static bool IsMonoVorbis(ReadOnlySpan<byte> data)
    {
        return TryGetChannelCount(data, out var channels) && channels == 1;
    }

    public static bool TryGetChannelCount(ReadOnlySpan<byte> data, out int channels)
    {
        channels = 0;

        if (data.Length < 64 ||
            data[0] != (byte) 'O' ||
            data[1] != (byte) 'g' ||
            data[2] != (byte) 'g' ||
            data[3] != (byte) 'S')
        {
            return false;
        }

        var limit = Math.Min(data.Length - 12, 4096);
        for (var i = 0; i < limit; i++)
        {
            if (data[i] != 1 ||
                data[i + 1] != (byte) 'v' ||
                data[i + 2] != (byte) 'o' ||
                data[i + 3] != (byte) 'r' ||
                data[i + 4] != (byte) 'b' ||
                data[i + 5] != (byte) 'i' ||
                data[i + 6] != (byte) 's')
            {
                continue;
            }

            channels = data[i + 11];
            return channels is > 0 and <= 8;
        }

        return false;
    }
}
