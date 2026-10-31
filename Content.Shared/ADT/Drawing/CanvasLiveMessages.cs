using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Drawing;

public enum CanvasStrokePhase : byte
{
    Begin,
    Points,
    End,
}

public readonly record struct CanvasStrokeBrush(
    uint Rgba,
    float Size,
    float Hardness,
    float Opacity,
    float Spacing,
    float MinSizeFraction,
    float MinOpacityFraction,
    bool PressureAffectsSize,
    bool PressureAffectsOpacity,
    bool Erase,
    bool Pixel);

public readonly record struct CanvasStrokeSample(int Dx, int Dy, byte Pressure, byte Dt);

public sealed class MsgCanvasStroke : NetMessage
{
    private const float MaxCoordinate = CanvasConstants.MaxSide * 2f * CanvasConstants.CoordScale;

    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public override int SequenceChannel => CanvasNetChannels.Session;

    public Guid Painting;
    public byte Author = CanvasConstants.NoAuthor;
    public uint StrokeId;
    public uint Time;
    public CanvasStrokePhase Phase;
    public CanvasStrokeBrush Brush;
    public float TaperOut;
    public int StartX;
    public int StartY;
    public byte StartPressure;
    public List<CanvasStrokeSample> Samples = new();
    public bool Malformed;

    public override int EstimateBufferSize()
    {
        return 64 + Samples.Count * 6;
    }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();
        Author = buffer.ReadByte();
        StrokeId = buffer.ReadUInt32();
        Time = buffer.ReadUInt32();
        Phase = (CanvasStrokePhase) buffer.ReadByte();

        switch (Phase)
        {
            case CanvasStrokePhase.Begin:
                ReadBegin(buffer);
                break;

            case CanvasStrokePhase.Points:
                ReadPoints(buffer);
                break;

            case CanvasStrokePhase.End:
                TaperOut = buffer.ReadFloat();
                Malformed = !InRange(TaperOut, 0f, CanvasConstants.MaxSide);
                break;

            default:
                Malformed = true;
                break;
        }

        if (Malformed)
            buffer.Position = buffer.LengthBits;
    }

    private void ReadBegin(NetIncomingMessage buffer)
    {
        var rgba = buffer.ReadUInt32();
        var size = buffer.ReadFloat();
        var hardness = buffer.ReadFloat();
        var opacity = buffer.ReadFloat();
        var spacing = buffer.ReadFloat();
        var minSize = buffer.ReadFloat();
        var minOpacity = buffer.ReadFloat();
        var flags = buffer.ReadByte();
        TaperOut = buffer.ReadFloat();
        StartX = buffer.ReadVariableInt32();
        StartY = buffer.ReadVariableInt32();
        StartPressure = buffer.ReadByte();

        Brush = new CanvasStrokeBrush(rgba, size, hardness, opacity, spacing, minSize, minOpacity,
            (flags & 1) != 0, (flags & 2) != 0, (flags & 4) != 0, (flags & 8) != 0);

        Malformed = !InRange(size, 0f, CanvasConstants.MaxBrushSize) ||
                    !InRange(hardness, 0f, 1f) ||
                    !InRange(opacity, 0f, 1f) ||
                    !InRange(spacing, 0.01f, 4f) ||
                    !InRange(minSize, 0f, 1f) ||
                    !InRange(minOpacity, 0f, 1f) ||
                    !InRange(TaperOut, 0f, CanvasConstants.MaxSide) ||
                    !InRange(StartX, -MaxCoordinate, MaxCoordinate) ||
                    !InRange(StartY, -MaxCoordinate, MaxCoordinate);
    }

    private void ReadPoints(NetIncomingMessage buffer)
    {
        var count = buffer.ReadVariableInt32();
        if (count < 0 || count > CanvasConstants.MaxStrokeSamples || !CanvasNetIO.HasBits(buffer, count * 4L))
        {
            Malformed = true;
            return;
        }

        Samples = new List<CanvasStrokeSample>(count);
        for (var i = 0; i < count; i++)
        {
            var dx = buffer.ReadVariableInt32();
            var dy = buffer.ReadVariableInt32();
            var pressure = buffer.ReadByte();
            var dt = buffer.ReadByte();
            if (!InRange(dx, -MaxCoordinate, MaxCoordinate) || !InRange(dy, -MaxCoordinate, MaxCoordinate))
            {
                Malformed = true;
                return;
            }

            Samples.Add(new CanvasStrokeSample(dx, dy, pressure, dt));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        buffer.Write(Author);
        buffer.Write(StrokeId);
        buffer.Write(Time);
        buffer.Write((byte) Phase);

        switch (Phase)
        {
            case CanvasStrokePhase.Begin:
                var flags = (byte) ((Brush.PressureAffectsSize ? 1 : 0) |
                                    (Brush.PressureAffectsOpacity ? 2 : 0) |
                                    (Brush.Erase ? 4 : 0) |
                                    (Brush.Pixel ? 8 : 0));
                buffer.Write(Brush.Rgba);
                buffer.Write(Brush.Size);
                buffer.Write(Brush.Hardness);
                buffer.Write(Brush.Opacity);
                buffer.Write(Brush.Spacing);
                buffer.Write(Brush.MinSizeFraction);
                buffer.Write(Brush.MinOpacityFraction);
                buffer.Write(flags);
                buffer.Write(TaperOut);
                buffer.WriteVariableInt32(StartX);
                buffer.WriteVariableInt32(StartY);
                buffer.Write(StartPressure);
                break;

            case CanvasStrokePhase.Points:
                buffer.WriteVariableInt32(Samples.Count);
                foreach (var sample in Samples)
                {
                    buffer.WriteVariableInt32(sample.Dx);
                    buffer.WriteVariableInt32(sample.Dy);
                    buffer.Write(sample.Pressure);
                    buffer.Write(sample.Dt);
                }
                break;

            case CanvasStrokePhase.End:
                buffer.Write(TaperOut);
                break;
        }
    }

    private static bool InRange(float value, float min, float max)
    {
        return float.IsFinite(value) && value >= min && value <= max;
    }
}

[Flags]
public enum CanvasCursorFlags : byte
{
    None = 0,
    Visible = 1,
    Drawing = 2,
    Eraser = 4,
    Picker = 8,
}

public readonly record struct CanvasCursorSample(ushort X, ushort Y, byte Dt);

public sealed class MsgCanvasCursor : NetMessage
{
    public const int MaxSamples = 16;

    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.UnreliableSequenced;

    public override int SequenceChannel => CanvasNetChannels.Cursor;

    public Guid Painting;
    public byte Author = CanvasConstants.NoAuthor;
    public uint Time;
    public CanvasCursorFlags Flags;
    public ushort Size;
    public uint Rgba;
    public List<CanvasCursorSample> Samples = new();
    public bool Malformed;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Painting = buffer.ReadGuid();
        Author = buffer.ReadByte();
        Time = buffer.ReadUInt32();
        Flags = (CanvasCursorFlags) buffer.ReadByte();
        if ((Flags & CanvasCursorFlags.Visible) == 0)
            return;

        Size = buffer.ReadUInt16();
        Rgba = buffer.ReadUInt32();

        var count = buffer.ReadByte();
        if (count == 0 || count > MaxSamples || !CanvasNetIO.HasBits(buffer, count * 5L))
        {
            Malformed = true;
            buffer.Position = buffer.LengthBits;
            return;
        }

        Samples = new List<CanvasCursorSample>(count);
        for (var i = 0; i < count; i++)
        {
            var x = buffer.ReadUInt16();
            var y = buffer.ReadUInt16();
            var dt = buffer.ReadByte();
            Samples.Add(new CanvasCursorSample(x, y, dt));
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Painting);
        buffer.Write(Author);
        buffer.Write(Time);
        buffer.Write((byte) Flags);
        if ((Flags & CanvasCursorFlags.Visible) == 0)
            return;

        buffer.Write(Size);
        buffer.Write(Rgba);
        buffer.Write((byte) Samples.Count);
        foreach (var sample in Samples)
        {
            buffer.Write(sample.X);
            buffer.Write(sample.Y);
            buffer.Write(sample.Dt);
        }
    }
}
