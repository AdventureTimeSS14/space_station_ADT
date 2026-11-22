namespace Content.Client.ADT.InconnuOS.UI;

public static class OsFormat
{
    private const long Kilobyte = 1024;

    private static readonly string[] Units =
    {
        "os-size-kilobytes",
        "os-size-megabytes",
        "os-size-gigabytes",
        "os-size-terabytes",
        "os-size-petabytes",
    };

    public static string Size(long bytes)
    {
        if (bytes < Kilobyte)
            return Loc.GetString("os-size-bytes", ("size", bytes));

        var value = bytes / (double) Kilobyte;
        var unit = 0;

        while (value >= Kilobyte && unit < Units.Length - 1)
        {
            value /= Kilobyte;
            unit++;
        }

        return Loc.GetString(Units[unit], ("size", Number(value)));
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
