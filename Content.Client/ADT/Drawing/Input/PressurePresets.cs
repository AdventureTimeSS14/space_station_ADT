using Content.Client.ADT.Drawing.Canvas;

namespace Content.Client.ADT.Drawing.Input;

public enum PressurePreset : byte
{
    Liner,
    Marker,
    Brush,
    Pencil,
    Custom,
}

public static class PressurePresets
{
    public static void Apply(PressurePreset preset, PenInputSystem pen, BrushSettings brush)
    {
        var sim = pen.SimulationTemplate;

        switch (preset)
        {
            case PressurePreset.Liner:
                pen.SimulatePressure = true;
                Set(sim, 1f, 0.25f, 3f, 24f, 32f, 0.75f);
                brush.PressureAffectsSize = true;
                brush.PressureAffectsOpacity = false;
                break;

            case PressurePreset.Marker:
                pen.SimulatePressure = false;
                Set(sim, 1f, 1f, 4f, 0f, 0f, 0.75f);
                brush.PressureAffectsSize = false;
                brush.PressureAffectsOpacity = false;
                break;

            case PressurePreset.Brush:
                pen.SimulatePressure = true;
                Set(sim, 1f, 0.55f, 5f, 16f, 24f, 0.85f);
                brush.PressureAffectsSize = true;
                brush.PressureAffectsOpacity = true;
                break;

            case PressurePreset.Pencil:
                pen.SimulatePressure = true;
                Set(sim, 0.9f, 0.6f, 6f, 8f, 8f, 0.6f);
                brush.PressureAffectsSize = false;
                brush.PressureAffectsOpacity = true;
                break;
        }
    }

    private static void Set(SimulatedPressure sim, float slow, float fast, float fastSpeed, float taperIn, float taperOut, float smoothing)
    {
        sim.SlowPressure = slow;
        sim.FastPressure = fast;
        sim.FastSpeed = fastSpeed;
        sim.TaperInLength = taperIn;
        sim.TaperOutLength = taperOut;
        sim.SpeedSmoothing = smoothing;
    }
}
