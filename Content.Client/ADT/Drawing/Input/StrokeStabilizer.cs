using System.Numerics;

namespace Content.Client.ADT.Drawing.Input;

public sealed class StrokeStabilizer
{
    public float StringLength;

    private Vector2 _brush;
    private bool _active;

    public void Begin(Vector2 position)
    {
        _brush = position;
        _active = true;
    }

    public bool Move(Vector2 pointer, out Vector2 brush)
    {
        brush = _brush;
        if (!_active)
            return false;

        if (StringLength <= 0f)
        {
            _brush = pointer;
            brush = pointer;
            return true;
        }

        var delta = pointer - _brush;
        var length = delta.Length();
        if (length <= StringLength)
            return false;

        _brush += delta * ((length - StringLength) / length);
        brush = _brush;
        return true;
    }

    public Vector2 End(Vector2 pointer)
    {
        _active = false;
        _brush = pointer;
        return pointer;
    }
}
