namespace Content.Client.ADT.Drawing.Input;

public sealed class SimulatedPressure
{
    public float SlowPressure = 1f;
    public float FastPressure = 0.35f;
    public float FastSpeed = 4f;
    public float TaperInLength = 24f;
    public float TaperOutLength = 32f;
    public float SpeedSmoothing = 0.75f;

    private float _speed;
    private float _distance;

    public void CopySettingsFrom(SimulatedPressure other)
    {
        SlowPressure = other.SlowPressure;
        FastPressure = other.FastPressure;
        FastSpeed = other.FastSpeed;
        TaperInLength = other.TaperInLength;
        TaperOutLength = other.TaperOutLength;
        SpeedSmoothing = other.SpeedSmoothing;
    }

    public void Reset()
    {
        _speed = 0f;
        _distance = 0f;
    }

    public float Next(float screenDistance, TimeSpan dt)
    {
        if (!float.IsFinite(screenDistance) || screenDistance < 0f)
            screenDistance = 0f;

        _distance += screenDistance;

        var ms = MathF.Max((float) dt.TotalMilliseconds, 2f);
        var instant = screenDistance / ms;
        var smoothing = Math.Clamp(SpeedSmoothing, 0f, 0.99f);
        _speed = _speed * smoothing + instant * (1f - smoothing);

        var t = FastSpeed <= 0f ? 0f : SmoothStep(Math.Clamp(_speed / FastSpeed, 0f, 1f));
        var pressure = SlowPressure + (FastPressure - SlowPressure) * t;

        if (TaperInLength > 0f)
            pressure *= SmoothStep(Math.Clamp(_distance / TaperInLength, 0f, 1f)) * 0.8f + 0.2f;

        return float.IsFinite(pressure) ? Math.Clamp(pressure, 0f, 1f) : 1f;
    }

    private static float SmoothStep(float x)
    {
        return x * x * (3f - 2f * x);
    }
}
