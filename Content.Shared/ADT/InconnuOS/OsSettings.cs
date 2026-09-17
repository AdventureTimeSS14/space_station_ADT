using Robust.Shared.Serialization;

namespace Content.Shared.ADT.InconnuOS;

[Serializable, NetSerializable]
public enum OsWallpaper : byte
{
    Depths = 0,
    Grid,
    Aurora,
    Circuitry,
    Plain,
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class OsSettings
{
    [DataField]
    public OsWallpaper Wallpaper = OsWallpaper.Depths;

    [DataField]
    public Color Accent = Color.FromHex("#3f8fd0");

    [DataField]
    public bool Sounds = true;

    [DataField]
    public float Volume = 0.5f;

    [DataField]
    public bool Animations = true;

    [DataField]
    public bool ShowClock = true;

    public OsSettings Clone()
    {
        return new OsSettings
        {
            Wallpaper = Wallpaper,
            Accent = Accent,
            Sounds = Sounds,
            Volume = Volume,
            Animations = Animations,
            ShowClock = ShowClock,
        };
    }
}
