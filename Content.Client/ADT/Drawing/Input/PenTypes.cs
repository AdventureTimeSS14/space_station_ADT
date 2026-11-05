using System.Numerics;

namespace Content.Client.ADT.Drawing.Input;

public enum PenTool : byte
{
    Pen,
    Eraser,
}

public enum PenPhase : byte
{
    Hover,
    Down,
    Move,
    Up,
}

public enum PressureOrigin : byte
{
    Constant,
    Simulated,
}

public readonly record struct PenSample(
    Vector2 Position,
    float Pressure,
    float RawPressure,
    PenTool Tool,
    PenPhase Phase,
    PressureOrigin Origin,
    TimeSpan Time);
