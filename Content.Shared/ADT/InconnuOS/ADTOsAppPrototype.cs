using System.Numerics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.InconnuOS;

[Serializable, NetSerializable]
public enum OsAppCategory : byte
{
    System = 0,
    Tools,
    Service,
    Other,
}

[Serializable, NetSerializable]
public enum OsAppIcon : byte
{
    Generic = 0,
    Computer,
    Folder,
    Terminal,
    Circuit,
    Notepad,
    Settings,
    Tasks,
    Devices,
    Disk,
}

[Prototype("osApp")]
public sealed partial class ADTOsAppPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    [DataField]
    public LocId? Description;

    [DataField]
    public OsAppCategory Category = OsAppCategory.Other;

    [DataField]
    public OsAppIcon Icon = OsAppIcon.Generic;

    /// <summary>
    /// Имя клиентского класса-наследника OsAppControl
    /// </summary>
    [DataField(required: true)]
    public string Window = default!;

    [DataField]
    public Vector2 MinSize = new(420f, 280f);

    [DataField]
    public Vector2 DefaultSize = new(720f, 480f);

    [DataField]
    public bool SingleInstance = true;

    [DataField]
    public bool OnDesktop;

    [DataField]
    public int Priority;

    [DataField]
    public List<string> Handles = new();

    [DataField]
    public string? RequiresComponent;

    [DataField]
    public bool Hidden;
}
