using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.InconnuOS.Components;

[RegisterComponent]
public sealed partial class ADTOperatingSystemComponent : Component
{
    [DataField]
    public OsDisk Disk = new();

    [DataField]
    public OsSettings Settings = new();

    [DataField]
    public List<ProtoId<ADTOsAppPrototype>> Apps = new();

    [DataField]
    public string MachineName = string.Empty;

    [DataField]
    public char Drive = 'C';

    [DataField]
    public string DriveLabel = "system";

    [DataField]
    public bool Activated;

    [DataField]
    public bool InstallDefaultFiles = true;

    [DataField]
    public bool DefaultFilesInstalled;

    [DataField]
    public int MaxFiles = 128;

    [DataField]
    public int MaxFileLength = 4096;

    [DataField]
    public int MaxPathLength = 128;

    [DataField]
    public int MaxTotalLength = 65536;

    [DataField]
    public int MaxHistory = 32;

    [DataField]
    public int MaxCommandLength = 256;

    [DataField]
    public SoundSpecifier? BootSound;

    [ViewVariables]
    public TimeSpan BootedAt;

    [ViewVariables]
    public bool Running;

    [ViewVariables]
    public bool Crashed;

    [ViewVariables]
    public bool UiOpen;

    [ViewVariables]
    public string WorkingDirectory = string.Empty;

    [ViewVariables]
    public string SessionUser = string.Empty;
}
