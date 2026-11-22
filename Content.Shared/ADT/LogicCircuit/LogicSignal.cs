using System.Globalization;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.LogicCircuit;

[Serializable, NetSerializable]
public enum LogicSignalKind : byte
{
    None = 0,
    Number,
    Text,
    NumericText,
}

[Serializable, NetSerializable]
public readonly struct LogicSignal : IEquatable<LogicSignal>
{
    public const int DefaultMaxLength = 64;
    private const string NumberFormat = "0.######";

    public static readonly LogicSignal Empty = default;
    public static readonly LogicSignal True = FromNumber(1f);
    public static readonly LogicSignal False = FromNumber(0f);

    public readonly LogicSignalKind Kind;
    public readonly float Number;
    public readonly string? Text;

    private LogicSignal(LogicSignalKind kind, float number, string? text)
    {
        Kind = kind;
        Number = number;
        Text = text;
    }

    public bool IsNumber => Kind is LogicSignalKind.Number or LogicSignalKind.NumericText;

    public bool IsEmpty => Kind == LogicSignalKind.None;

    public static LogicSignal FromNumber(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            value = 0f;

        return new LogicSignal(LogicSignalKind.Number, value, null);
    }

    public static LogicSignal FromBool(bool value)
    {
        return value ? True : False;
    }

    public static LogicSignal FromText(string? value, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrEmpty(value))
            return Empty;

        if (maxLength > 0 && value.Length > maxLength)
            value = value[..maxLength];

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            && !float.IsNaN(number)
            && !float.IsInfinity(number))
        {
            return new LogicSignal(LogicSignalKind.NumericText, number, value);
        }

        return new LogicSignal(LogicSignalKind.Text, 0f, value);
    }

    public float AsNumber()
    {
        return IsNumber ? Number : 0f;
    }

    public bool AsBool()
    {
        return Kind switch
        {
            LogicSignalKind.None => false,
            LogicSignalKind.Number => Number != 0f,
            LogicSignalKind.NumericText => Number != 0f,
            _ => true,
        };
    }

    public string AsText()
    {
        return Kind switch
        {
            LogicSignalKind.None => string.Empty,
            LogicSignalKind.Number => Number.ToString(NumberFormat, CultureInfo.InvariantCulture),
            _ => Text ?? string.Empty,
        };
    }

    public bool Equals(LogicSignal other)
    {
        if (Kind != other.Kind)
            return false;

        return Kind switch
        {
            LogicSignalKind.None => true,
            LogicSignalKind.Number => Number.Equals(other.Number),
            _ => string.Equals(Text, other.Text, StringComparison.Ordinal),
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is LogicSignal other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Kind switch
        {
            LogicSignalKind.None => 0,
            LogicSignalKind.Number => HashCode.Combine(Kind, Number),
            _ => HashCode.Combine(Kind, Text),
        };
    }

    public override string ToString()
    {
        return AsText();
    }

    public static bool operator ==(LogicSignal left, LogicSignal right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LogicSignal left, LogicSignal right)
    {
        return !left.Equals(right);
    }
}
