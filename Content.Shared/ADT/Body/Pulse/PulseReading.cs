using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Body.Pulse;

[Serializable, NetSerializable]
public enum PulseStrength : byte
{
    Absent,
    Regular,
    Weak,
    Irregular,
}

[Serializable, NetSerializable]
public enum PulsePressure : byte
{
    Strong,
    Thready,
}

[Serializable, NetSerializable]
public readonly record struct PulseReading(PulseStrength Strength, PulsePressure Pressure, int Bpm)
{
    public bool Normal => Strength == PulseStrength.Regular && Pressure == PulsePressure.Strong;

    public string Describe()
    {
        if (Strength == PulseStrength.Absent)
            return Loc.GetString("pulse-absent");

        return Loc.GetString("pulse-reading",
            ("bpm", Bpm),
            ("pressure", Loc.GetString($"pulse-pressure-{Pressure.ToString().ToLowerInvariant()}")),
            ("strength", Loc.GetString($"pulse-strength-{Strength.ToString().ToLowerInvariant()}")));
    }
}
