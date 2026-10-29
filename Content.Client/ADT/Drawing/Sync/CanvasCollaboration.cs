using System.Linq;
using System.Numerics;
using Content.Client.ADT.Drawing.Canvas;
using Content.Client.ADT.Drawing.Input;
using Content.Shared.ADT.Drawing;
using Robust.Client.Graphics;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Drawing.Sync;

public sealed class CanvasRemoteStroke
{
    public readonly HashSet<int> Tiles = new();
    public double LastActivity;
    public bool Finished;
}

public enum CanvasRemoteEventKind : byte
{
    Begin,
    Point,
    End,
}

public readonly record struct CanvasRemoteEvent(
    double At,
    CanvasRemoteEventKind Kind,
    uint StrokeId,
    int X,
    int Y,
    byte Pressure,
    float TaperOut,
    CanvasStrokeBrush Brush);

public readonly record struct CanvasCursorKey(
    double At,
    Vector2 Position,
    bool Hide,
    CanvasCursorFlags Flags,
    float Size,
    Color BrushColor);

public sealed class CanvasRemoteParticipant
{
    public readonly byte Index;
    public readonly Dictionary<uint, CanvasRemoteStroke> Strokes = new();
    public readonly HashSet<int> PendingClear = new();
    public readonly Queue<CanvasRemoteEvent> Events = new();
    public readonly Queue<CanvasCursorKey> Keys = new();

    public string Name = "?";
    public CanvasRole Role;
    public bool Listed;
    public Color Color;
    public PaintSurface? Preview;
    public BrushStroker? Stroker;
    public uint? ActiveStroke;
    public int LastX;
    public int LastY;

    public double? Offset;
    public double LastEventAt;
    public double LastKeyAt;
    public double LastMessage;

    public bool CursorVisible;
    public Vector2 CursorPosition;
    public float CursorSize;
    public CanvasCursorFlags CursorFlags;
    public Color BrushColor;
    public CanvasCursorKey? FromKey;

    public CanvasRemoteParticipant(byte index)
    {
        Index = index;
        Color = CanvasCollaboration.ColorOf(index);
    }
}

public sealed class CanvasCollaboration : IDisposable
{
    private const int MaxPointsPerFlush = 40;
    private const int MaxBacklog = 4096;
    private const double PlaybackDelay = 100;
    private const double OffsetReset = 5000;
    private const double OffsetDrift = 0.02;
    private const double StrokeFlushInterval = 50;
    private const double CursorInterval = 50;
    private const double CursorKeepAlive = 1000;
    private const double CursorTimeout = 3000;
    private const double PreviewTimeout = 10000;

    private static readonly Color[] Palette =
    {
        Color.FromHex("#e6194b"),
        Color.FromHex("#3cb44b"),
        Color.FromHex("#4363d8"),
        Color.FromHex("#f58231"),
        Color.FromHex("#911eb4"),
        Color.FromHex("#42d4f4"),
        Color.FromHex("#f032e6"),
        Color.FromHex("#9a6324"),
    };

    private readonly Guid _painting;
    private readonly CanvasDocument _document;
    private readonly Action<NetMessage> _send;
    private readonly IGameTiming _timing;
    private readonly Dictionary<byte, CanvasRemoteParticipant> _participants = new();

    private OutgoingStroke? _outgoing;

    private readonly List<(ushort X, ushort Y, uint Time)> _cursorSamples = new();
    private bool _cursorVisible;
    private bool _cursorSentVisible;
    private bool _hidePending;
    private CanvasCursorFlags _cursorFlags;
    private ushort _cursorSize;
    private uint _cursorRgba;
    private (ushort X, ushort Y) _cursorLast;
    private double _nextCursor;
    private double _cursorSentAt;

    public CanvasCollaboration(Guid painting, CanvasDocument document, Action<NetMessage> send, IGameTiming timing)
    {
        _painting = painting;
        _document = document;
        _send = send;
        _timing = timing;

        _document.Stroker.StrokeStarted += OnLocalStrokeStarted;
        _document.Stroker.PointAdded += OnLocalPoint;
        _document.Stroker.StrokeEnded += OnLocalStrokeEnded;
    }

    public CanvasRole LocalRole { get; private set; } = CanvasRole.Viewer;

    public byte? LocalIndex { get; private set; }

    public IEnumerable<CanvasRemoteParticipant> Participants => _participants.Values;

    private double Now => _timing.RealTime.TotalMilliseconds;

    private uint NowStamp => unchecked((uint) (long) _timing.RealTime.TotalMilliseconds);

    public static Color ColorOf(byte index)
    {
        return Palette[index % Palette.Length];
    }

    public void SetParticipants(List<CanvasParticipantInfo> participants, NetUserId? localUser)
    {
        LocalRole = CanvasRole.Viewer;
        LocalIndex = null;

        foreach (var participant in _participants.Values)
        {
            participant.Listed = false;
        }

        foreach (var info in participants)
        {
            if (info.User == localUser)
            {
                LocalRole = info.Role;
                LocalIndex = info.Index;
                continue;
            }

            if (!info.Present)
                continue;

            var participant = GetOrCreate(info.Index);
            participant.Name = info.Name;
            participant.Role = info.Role;
            participant.Listed = true;
        }

        foreach (var participant in _participants.Values.ToList())
        {
            if (participant.Listed ||
                participant.ActiveStroke != null ||
                participant.Strokes.Count > 0 ||
                participant.Events.Count > 0)
            {
                continue;
            }

            Remove(participant);
        }
    }

    private CanvasRemoteParticipant GetOrCreate(byte index)
    {
        if (_participants.TryGetValue(index, out var participant))
            return participant;

        participant = new CanvasRemoteParticipant(index);
        _participants[index] = participant;
        return participant;
    }

    private void Remove(CanvasRemoteParticipant participant)
    {
        _participants.Remove(participant.Index);
        participant.Preview?.Dispose();
    }

    #region Время

    private double Schedule(CanvasRemoteParticipant participant, uint stamp, uint lastStamp)
    {
        var now = Now;
        participant.LastMessage = now;

        var observed = now - lastStamp;
        if (participant.Offset is not { } offset || observed < offset || observed - offset > OffsetReset)
            participant.Offset = observed;
        else
            participant.Offset = offset + (observed - offset) * OffsetDrift;

        return stamp + participant.Offset.Value + PlaybackDelay;
    }

    private static void Enqueue(CanvasRemoteParticipant participant, CanvasRemoteEvent ev)
    {
        var at = Math.Max(ev.At, participant.LastEventAt);
        participant.LastEventAt = at;
        participant.Events.Enqueue(ev with { At = at });
    }

    private static void EnqueueKey(CanvasRemoteParticipant participant, CanvasCursorKey key)
    {
        var at = Math.Max(key.At, participant.LastKeyAt);
        participant.LastKeyAt = at;
        participant.Keys.Enqueue(key with { At = at });
    }

    #endregion

    #region Чужие штрихи

    public void HandleStroke(MsgCanvasStroke msg)
    {
        if (msg.Author == CanvasConstants.NoAuthor || msg.Author == LocalIndex)
            return;

        var participant = GetOrCreate(msg.Author);
        switch (msg.Phase)
        {
            case CanvasStrokePhase.Begin:
            {
                var at = Schedule(participant, msg.Time, msg.Time);
                Enqueue(participant, new CanvasRemoteEvent(at, CanvasRemoteEventKind.Begin, msg.StrokeId,
                    msg.StartX, msg.StartY, msg.StartPressure, msg.TaperOut, msg.Brush));
                break;
            }

            case CanvasStrokePhase.Points:
            {
                var last = msg.Time;
                foreach (var sample in msg.Samples)
                {
                    last += sample.Dt;
                }

                var at = Schedule(participant, msg.Time, last);
                foreach (var sample in msg.Samples)
                {
                    at += sample.Dt;
                    Enqueue(participant, new CanvasRemoteEvent(at, CanvasRemoteEventKind.Point, msg.StrokeId,
                        sample.Dx, sample.Dy, sample.Pressure, 0f, default));
                }
                break;
            }

            case CanvasStrokePhase.End:
            {
                var at = Schedule(participant, msg.Time, msg.Time);
                Enqueue(participant, new CanvasRemoteEvent(at, CanvasRemoteEventKind.End, msg.StrokeId,
                    0, 0, 0, msg.TaperOut, default));
                break;
            }
        }

        if (participant.Events.Count > MaxBacklog)
            PlayEvents(participant, double.MaxValue);
    }

    private void PlayEvents(CanvasRemoteParticipant participant, double now)
    {
        while (participant.Events.TryPeek(out var ev) && ev.At <= now)
        {
            participant.Events.Dequeue();
            switch (ev.Kind)
            {
                case CanvasRemoteEventKind.Begin:
                    BeginRemote(participant, ev);
                    break;

                case CanvasRemoteEventKind.Point:
                    if (participant.ActiveStroke != ev.StrokeId || participant.Stroker == null)
                        break;

                    participant.LastX += ev.X;
                    participant.LastY += ev.Y;
                    participant.Stroker.Handle(RemoteSample(participant.LastX, participant.LastY, ev.Pressure, PenPhase.Move), (int) ev.StrokeId);
                    Touch(participant, ev.StrokeId);
                    break;

                case CanvasRemoteEventKind.End:
                    if (participant.ActiveStroke != ev.StrokeId || participant.Stroker == null)
                        break;

                    participant.Stroker.TaperOutLength = ev.TaperOut;
                    EndRemote(participant);
                    break;
            }
        }
    }

    private void BeginRemote(CanvasRemoteParticipant participant, CanvasRemoteEvent ev)
    {
        if (participant.ActiveStroke != null)
            EndRemote(participant);

        if (participant.Preview == null)
        {
            participant.Preview = new PaintSurface(_document.Width, _document.Height, Color.Transparent, sparse: true)
            {
                History = false,
            };
            participant.Stroker = new BrushStroker(participant.Preview, new BrushSettings());
            participant.Stroker.StrokeFinished += (_, tag, tiles) => OnRemoteFinished(participant, (uint) tag, tiles);
        }

        var brush = ev.Brush;
        participant.Stroker!.Settings = new BrushSettings
        {
            Color = brush.Erase ? Color.White : ToColor(brush.Rgba),
            Size = brush.Size,
            Hardness = brush.Hardness,
            Opacity = brush.Opacity,
            Spacing = brush.Spacing,
            PressureAffectsSize = brush.PressureAffectsSize,
            PressureAffectsOpacity = brush.PressureAffectsOpacity,
            MinSizeFraction = brush.MinSizeFraction,
            MinOpacityFraction = brush.MinOpacityFraction,
            Pixel = brush.Pixel,
        };
        participant.Stroker.TaperOutLength = ev.TaperOut;

        participant.ActiveStroke = ev.StrokeId;
        participant.LastX = ev.X;
        participant.LastY = ev.Y;
        participant.Strokes[ev.StrokeId] = new CanvasRemoteStroke
        {
            LastActivity = Now,
        };

        participant.Stroker.Handle(RemoteSample(ev.X, ev.Y, ev.Pressure, PenPhase.Down), (int) ev.StrokeId);
    }

    private void EndRemote(CanvasRemoteParticipant participant)
    {
        if (participant.ActiveStroke is not { } id)
            return;

        participant.ActiveStroke = null;
        participant.Stroker?.End();

        if (participant.Strokes.TryGetValue(id, out var stroke))
        {
            stroke.Finished = true;
            stroke.LastActivity = Now;
            if (stroke.Tiles.Count == 0)
                participant.Strokes.Remove(id);
        }

        foreach (var index in participant.PendingClear.ToList())
        {
            participant.PendingClear.Remove(index);
            ClearIfUnused(participant, index);
        }
    }

    private void OnRemoteFinished(CanvasRemoteParticipant participant, uint id, int[] tiles)
    {
        if (participant.Strokes.TryGetValue(id, out var stroke))
            stroke.Tiles.UnionWith(tiles);
    }

    private void Touch(CanvasRemoteParticipant participant, uint id)
    {
        if (participant.Strokes.TryGetValue(id, out var stroke))
            stroke.LastActivity = Now;
    }

    public void TileConfirmed(byte author, uint[] strokeIds, int index)
    {
        if (!_participants.TryGetValue(author, out var participant) || participant.Preview == null)
            return;

        foreach (var id in strokeIds)
        {
            if (!participant.Strokes.TryGetValue(id, out var stroke))
                continue;

            stroke.Tiles.Remove(index);
            if (stroke.Finished && stroke.Tiles.Count == 0)
                participant.Strokes.Remove(id);
        }

        ClearIfUnused(participant, index);
    }

    private void ClearIfUnused(CanvasRemoteParticipant participant, int index)
    {
        if (participant.Preview == null || !participant.Preview.IsAllocated(index))
            return;

        if (participant.ActiveStroke != null)
        {
            participant.PendingClear.Add(index);
            return;
        }

        foreach (var stroke in participant.Strokes.Values)
        {
            if (stroke.Tiles.Contains(index))
                return;
        }

        participant.Preview.ResetTile(index);
    }

    private static PenSample RemoteSample(int x, int y, byte pressure, PenPhase phase)
    {
        var position = new Vector2(x / CanvasConstants.CoordScale, y / CanvasConstants.CoordScale);
        var p = pressure / 255f;
        return new PenSample(position, p, p, PenTool.Pen, phase, PressureOrigin.Constant, TimeSpan.Zero);
    }

    #endregion

    #region Курсоры

    public void HandleCursor(MsgCanvasCursor msg)
    {
        if (msg.Author == CanvasConstants.NoAuthor || msg.Author == LocalIndex)
            return;

        var participant = GetOrCreate(msg.Author);
        if ((msg.Flags & CanvasCursorFlags.Visible) == 0)
        {
            var hideAt = Schedule(participant, msg.Time, msg.Time);
            EnqueueKey(participant, new CanvasCursorKey(hideAt, participant.CursorPosition, true, msg.Flags, 0f, default));
            return;
        }

        var last = msg.Time;
        foreach (var sample in msg.Samples)
        {
            last += sample.Dt;
        }

        var at = Schedule(participant, msg.Time, last);
        var size = msg.Size / CanvasConstants.SizeScale;
        var color = ToColor(msg.Rgba);
        foreach (var sample in msg.Samples)
        {
            at += sample.Dt;
            var position = new Vector2(sample.X / CanvasConstants.CoordScale, sample.Y / CanvasConstants.CoordScale);
            EnqueueKey(participant, new CanvasCursorKey(at, position, false, msg.Flags, size, color));
        }

        while (participant.Keys.Count > MaxBacklog)
        {
            participant.Keys.Dequeue();
        }
    }

    public void Advance()
    {
        var now = Now;
        foreach (var participant in _participants.Values)
        {
            PlayEvents(participant, now);
            AdvanceCursor(participant, now);
        }
    }

    private static void AdvanceCursor(CanvasRemoteParticipant participant, double now)
    {
        while (participant.Keys.TryPeek(out var key) && key.At <= now)
        {
            participant.Keys.Dequeue();
            participant.FromKey = key;

            if (key.Hide)
            {
                participant.CursorVisible = false;
                continue;
            }

            participant.CursorVisible = true;
            participant.CursorPosition = key.Position;
            participant.CursorFlags = key.Flags;
            participant.CursorSize = key.Size;
            participant.BrushColor = key.BrushColor;
        }

        if (!participant.CursorVisible || participant.FromKey is not { } from)
            return;

        if (participant.ActiveStroke != null)
        {
            participant.CursorPosition = new Vector2(
                participant.LastX / CanvasConstants.CoordScale,
                participant.LastY / CanvasConstants.CoordScale);
            return;
        }

        if (!participant.Keys.TryPeek(out var next) || next.Hide || next.At <= from.At)
            return;

        var t = (float) Math.Clamp((now - from.At) / (next.At - from.At), 0, 1);
        participant.CursorPosition = Vector2.Lerp(from.Position, next.Position, t);
    }

    public void ReportCursor(Vector2? position, CanvasCursorFlags flags, float size, Color color)
    {
        if (position is not { } pos || pos.X < 0 || pos.Y < 0 || pos.X > _document.Width || pos.Y > _document.Height)
        {
            if (_cursorVisible)
            {
                _cursorVisible = false;
                _hidePending = true;
                _cursorSamples.Clear();
            }

            return;
        }

        var x = (ushort) Math.Clamp(MathF.Round(pos.X * CanvasConstants.CoordScale), 0, ushort.MaxValue);
        var y = (ushort) Math.Clamp(MathF.Round(pos.Y * CanvasConstants.CoordScale), 0, ushort.MaxValue);
        var sizeQ = (ushort) Math.Clamp(MathF.Round(size * CanvasConstants.SizeScale), 0, ushort.MaxValue);
        var rgba = CanvasTileCodec.PackRgba(PaintSurface.ToRgba(color));

        var changed = !_cursorVisible ||
                      _cursorLast != (x, y) ||
                      _cursorFlags != flags ||
                      _cursorSize != sizeQ ||
                      _cursorRgba != rgba;

        _cursorVisible = true;
        _hidePending = false;
        _cursorFlags = flags;
        _cursorSize = sizeQ;
        _cursorRgba = rgba;
        _cursorLast = (x, y);

        if (changed)
            _cursorSamples.Add((x, y, NowStamp));
    }

    private void UpdateCursor()
    {
        var now = Now;
        if (now < _nextCursor)
            return;

        if (_cursorSamples.Count > 0)
        {
            SendCursorSamples();
        }
        else if (_hidePending)
        {
            _hidePending = false;
            _cursorSentVisible = false;
            _send(new MsgCanvasCursor
            {
                Painting = _painting,
                Time = NowStamp,
                Flags = CanvasCursorFlags.None,
            });
        }
        else if (_cursorVisible && _cursorSentVisible && now - _cursorSentAt >= CursorKeepAlive)
        {
            _cursorSamples.Add((_cursorLast.X, _cursorLast.Y, NowStamp));
            SendCursorSamples();
        }
        else
        {
            return;
        }

        _nextCursor = now + CursorInterval;
    }

    private void SendCursorSamples()
    {
        var picked = Decimate(_cursorSamples.Count, MsgCanvasCursor.MaxSamples);
        var samples = new List<CanvasCursorSample>(picked.Count);
        var first = _cursorSamples[picked[0]].Time;
        var previous = first;
        foreach (var i in picked)
        {
            var sample = _cursorSamples[i];
            samples.Add(new CanvasCursorSample(sample.X, sample.Y, StampDelta(previous, sample.Time)));
            previous = sample.Time;
        }

        _cursorSamples.Clear();
        _cursorSentVisible = true;
        _cursorSentAt = Now;

        _send(new MsgCanvasCursor
        {
            Painting = _painting,
            Time = first,
            Flags = _cursorFlags | CanvasCursorFlags.Visible,
            Size = _cursorSize,
            Rgba = _cursorRgba,
            Samples = samples,
        });
    }

    #endregion

    #region Свои штрихи

    private void OnLocalStrokeStarted(StrokeRecord stroke, int tag)
    {
        if (LocalRole == CanvasRole.Viewer || stroke.Points.Count == 0)
            return;

        var start = stroke.Points[0];
        var x = Quantize(start.Position.X);
        var y = Quantize(start.Position.Y);
        var settings = stroke.Settings;
        var now = Now;

        _outgoing = new OutgoingStroke((uint) tag, x, y)
        {
            NextFlush = now + StrokeFlushInterval,
        };

        _send(new MsgCanvasStroke
        {
            Painting = _painting,
            StrokeId = (uint) tag,
            Time = NowStamp,
            Phase = CanvasStrokePhase.Begin,
            Brush = new CanvasStrokeBrush(
                CanvasTileCodec.PackRgba(PaintSurface.ToRgba(settings.Color)),
                Math.Clamp(settings.Size, 0f, CanvasConstants.MaxBrushSize),
                Math.Clamp(settings.Hardness, 0f, 1f),
                Math.Clamp(settings.Opacity, 0f, 1f),
                Math.Clamp(settings.Spacing, 0.01f, 4f),
                Math.Clamp(settings.MinSizeFraction, 0f, 1f),
                Math.Clamp(settings.MinOpacityFraction, 0f, 1f),
                settings.PressureAffectsSize,
                settings.PressureAffectsOpacity,
                stroke.Erase,
                settings.Pixel),
            TaperOut = Math.Clamp(_document.Stroker.TaperOutLength, 0f, CanvasConstants.MaxSide),
            StartX = x,
            StartY = y,
            StartPressure = ToByte(start.Pressure),
        });
    }

    private void OnLocalPoint(StrokePoint point)
    {
        if (_outgoing == null)
            return;

        _outgoing.Points.Add((point, NowStamp));
        if (_outgoing.Points.Count >= CanvasConstants.MaxStrokeSamples)
            FlushStroke(_outgoing);
    }

    private void OnLocalStrokeEnded(int tag)
    {
        if (_outgoing is not { } outgoing || outgoing.Id != (uint) tag)
            return;

        FlushStroke(outgoing);
        _outgoing = null;

        _send(new MsgCanvasStroke
        {
            Painting = _painting,
            StrokeId = outgoing.Id,
            Time = NowStamp,
            Phase = CanvasStrokePhase.End,
            TaperOut = Math.Clamp(_document.Stroker.TaperOutLength, 0f, CanvasConstants.MaxSide),
        });
    }

    private void FlushStroke(OutgoingStroke outgoing)
    {
        outgoing.NextFlush = Now + StrokeFlushInterval;
        if (outgoing.Points.Count == 0)
            return;

        var points = outgoing.Points;
        var picked = Decimate(points.Count, MaxPointsPerFlush);
        var samples = new List<CanvasStrokeSample>(picked.Count);
        var first = points[picked[0]].Time;
        var previous = first;

        foreach (var i in picked)
        {
            var (point, time) = points[i];
            var x = Quantize(point.Position.X);
            var y = Quantize(point.Position.Y);
            samples.Add(new CanvasStrokeSample(x - outgoing.LastX, y - outgoing.LastY, ToByte(point.Pressure), StampDelta(previous, time)));
            outgoing.LastX = x;
            outgoing.LastY = y;
            previous = time;
        }

        points.Clear();
        _send(new MsgCanvasStroke
        {
            Painting = _painting,
            StrokeId = outgoing.Id,
            Time = first,
            Phase = CanvasStrokePhase.Points,
            Samples = samples,
        });
    }

    private sealed class OutgoingStroke
    {
        public readonly uint Id;
        public readonly List<(StrokePoint Point, uint Time)> Points = new();
        public int LastX;
        public int LastY;
        public double NextFlush;

        public OutgoingStroke(uint id, int x, int y)
        {
            Id = id;
            LastX = x;
            LastY = y;
        }
    }

    #endregion

    public void Update()
    {
        var now = Now;

        if (_outgoing is { } outgoing && now >= outgoing.NextFlush)
            FlushStroke(outgoing);

        UpdateCursor();

        foreach (var participant in _participants.Values)
        {
            if (participant.CursorVisible && participant.Keys.Count == 0 && now - participant.LastMessage > CursorTimeout)
                participant.CursorVisible = false;

            if (participant.ActiveStroke is { } active &&
                participant.Events.Count == 0 &&
                participant.Strokes.TryGetValue(active, out var current) &&
                now - current.LastActivity > PreviewTimeout)
            {
                EndRemote(participant);
            }

            if (participant.Strokes.Count == 0 || participant.ActiveStroke != null)
                continue;

            foreach (var (id, stroke) in participant.Strokes.ToList())
            {
                if (!stroke.Finished || now - stroke.LastActivity <= PreviewTimeout)
                    continue;

                participant.Strokes.Remove(id);
                foreach (var index in stroke.Tiles)
                {
                    ClearIfUnused(participant, index);
                }
            }
        }
    }

    public void FlushPreviews(IClyde clyde)
    {
        foreach (var participant in _participants.Values)
        {
            participant.Preview?.Flush(clyde);
        }
    }

    public void Dispose()
    {
        _document.Stroker.StrokeStarted -= OnLocalStrokeStarted;
        _document.Stroker.PointAdded -= OnLocalPoint;
        _document.Stroker.StrokeEnded -= OnLocalStrokeEnded;

        foreach (var participant in _participants.Values)
        {
            participant.Preview?.Dispose();
        }

        _participants.Clear();
    }

    private static List<int> Decimate(int count, int max)
    {
        var result = new List<int>(Math.Min(count, max));
        if (count <= max)
        {
            for (var i = 0; i < count; i++)
            {
                result.Add(i);
            }

            return result;
        }

        for (var k = 1; k <= max; k++)
        {
            result.Add((int) ((long) k * count / max) - 1);
        }

        return result;
    }

    private static byte StampDelta(uint from, uint to)
    {
        return (byte) Math.Clamp(unchecked((int) (to - from)), 0, byte.MaxValue);
    }

    private static int Quantize(float value)
    {
        return (int) MathF.Round(value * CanvasConstants.CoordScale);
    }

    private static byte ToByte(float value)
    {
        return (byte) Math.Clamp(MathF.Round(value * 255f), 0f, 255f);
    }

    private static Color ToColor(uint rgba)
    {
        var c = CanvasTileCodec.UnpackRgba(rgba);
        return new Color(c.R, c.G, c.B, c.A);
    }
}
