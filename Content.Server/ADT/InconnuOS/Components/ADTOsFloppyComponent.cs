using Content.Shared.ADT.InconnuOS;

namespace Content.Server.ADT.InconnuOS.Components;

[RegisterComponent]
public sealed partial class ADTOsFloppyComponent : Component
{
    [DataField]
    public OsDisk Disk = new();

    [DataField]
    public char Drive = 'A';

    [DataField]
    public string Label = "floppy";

    [DataField]
    public bool ReadOnly;

    [DataField]
    public int MaxFiles = 16;

    [DataField]
    public int MaxFileLength = 2048;

    [DataField]
    public int MaxPathLength = 64;

    [DataField]
    public int MaxTotalLength = 8192;
}
