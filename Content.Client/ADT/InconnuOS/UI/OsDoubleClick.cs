using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

public static class OsDoubleClick
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(0.45);

    private static object? _lastKey;
    private static TimeSpan _lastTime;

    public static bool Check(object key)
    {
        var now = IoCManager.Resolve<IGameTiming>().RealTime;
        var isDouble = _lastKey != null && _lastKey.Equals(key) && now - _lastTime <= Interval;

        if (isDouble)
        {
            _lastKey = null;
            return true;
        }

        _lastKey = key;
        _lastTime = now;
        return false;
    }
}
