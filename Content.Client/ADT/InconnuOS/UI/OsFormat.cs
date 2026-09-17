namespace Content.Client.ADT.InconnuOS.UI;

public static class OsFormat
{
    private const long Kilobyte = 1024;
    private const long Megabyte = 1024 * 1024;

    public static string Size(long bytes)
    {
        if (bytes < Kilobyte)
            return Loc.GetString("os-size-bytes", ("size", bytes));

        if (bytes < Megabyte)
            return Loc.GetString("os-size-kilobytes", ("size", Number(bytes / (double) Kilobyte)));

        return Loc.GetString("os-size-megabytes", ("size", Number(bytes / (double) Megabyte)));
    }

    private static string Number(double value)
    {
        if (value >= 100)
            return value.ToString("0");

        return value.ToString("0.#");
    }

    public static string Time(TimeSpan time)
    {
        var hours = (int) time.TotalHours;

        return $"{hours:00}:{time.Minutes:00}:{time.Seconds:00}";
    }
}
