using Content.Server.ADT.InconnuOS.Components;
using Content.Server.ADT.LogicCircuit.Components;
using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.InconnuOS.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Containers;

namespace Content.Server.ADT.InconnuOS;

public sealed partial class ADTOsSystem
{
    [Dependency] private readonly ItemSlotsSystem _slots = default!;

    public const string DriveSlot = "adt_os_drive";
    private const string DefaultCircuitName = "схема";

    partial void InitializeDrives()
    {
        SubscribeLocalEvent<ADTOperatingSystemComponent, EntInsertedIntoContainerMessage>(OnSlotChanged);
        SubscribeLocalEvent<ADTOperatingSystemComponent, EntRemovedFromContainerMessage>(OnSlotChanged);
    }

    private void OnSlotChanged<T>(Entity<ADTOperatingSystemComponent> ent, ref T args) where T : ContainerModifiedMessage
    {
        if (args.Container.ID != DriveSlot)
            return;

        UpdateUiState(ent);
    }

    public Entity<ADTOsFloppyComponent>? GetFloppy(EntityUid uid)
    {
        if (_slots.GetItemOrNull(uid, DriveSlot) is not { } item)
            return null;

        if (!TryComp<ADTOsFloppyComponent>(item, out var floppy))
            return null;

        return (item, floppy);
    }

    public OsDriveState[] BuildDrives(Entity<ADTOperatingSystemComponent> ent)
    {
        var comp = ent.Comp;

        var system = new OsDriveState(
            comp.Drive,
            comp.DriveLabel,
            comp.Disk,
            false,
            false,
            comp.MaxTotalLength);

        if (GetFloppy(ent.Owner) is not { } floppy)
            return new[] { system };

        SyncFloppyFromCircuit(floppy);

        var removable = new OsDriveState(
            floppy.Comp.Drive,
            floppy.Comp.Label,
            floppy.Comp.Disk,
            true,
            floppy.Comp.ReadOnly,
            floppy.Comp.MaxTotalLength);

        return new[] { system, removable };
    }

    public bool TryGetDrive(
        Entity<ADTOperatingSystemComponent> ent,
        char letter,
        out OsDisk disk,
        out OsLimits limits,
        out bool readOnly,
        out OsValidationError error)
    {
        var comp = ent.Comp;

        error = OsValidationError.None;

        if (letter == char.ToUpperInvariant(comp.Drive))
        {
            disk = comp.Disk;
            limits = GetLimits(comp);
            readOnly = false;
            return true;
        }

        if (GetFloppy(ent.Owner) is { } floppy && letter == char.ToUpperInvariant(floppy.Comp.Drive))
        {
            SyncFloppyFromCircuit(floppy);

            disk = floppy.Comp.Disk;
            limits = new OsLimits
            {
                MaxFiles = floppy.Comp.MaxFiles,
                MaxFileLength = floppy.Comp.MaxFileLength,
                MaxPathLength = floppy.Comp.MaxPathLength,
                MaxTotalLength = floppy.Comp.MaxTotalLength,
            };
            readOnly = floppy.Comp.ReadOnly;
            return true;
        }

        disk = default!;
        limits = default;
        readOnly = true;
        error = OsValidationError.UnknownDrive;
        return false;
    }

    private void SyncFloppyFromCircuit(Entity<ADTOsFloppyComponent> floppy)
    {
        if (!TryComp<ADTLogicDiskComponent>(floppy.Owner, out var circuit) || circuit.Layout == null)
            return;

        foreach (var file in floppy.Comp.Disk.Files)
        {
            if (file.Kind == OsFileKind.Circuit)
                return;
        }

        var name = circuit.Label;

        if (string.IsNullOrWhiteSpace(name) || !OsPath.IsValidName(name))
            name = DefaultCircuitName;

        floppy.Comp.Disk.Files.Add(new OsFile
        {
            Path = OsPath.GetRoot(floppy.Comp.Drive) + name + OsPath.ExtensionForKind(OsFileKind.Circuit),
            Kind = OsFileKind.Circuit,
            Circuit = circuit.Layout.Clone(),
            Modified = _timing.CurTime,
        });
    }

    private void SyncCircuitFromFloppy(Entity<ADTOsFloppyComponent> floppy)
    {
        if (!TryComp<ADTLogicDiskComponent>(floppy.Owner, out var circuit))
            return;

        OsFile? latest = null;

        foreach (var file in floppy.Comp.Disk.Files)
        {
            if (file.Kind != OsFileKind.Circuit || file.Circuit == null)
                continue;

            if (latest == null || file.Modified > latest.Modified)
                latest = file;
        }

        if (latest?.Circuit == null)
        {
            circuit.Layout = null;
            circuit.Label = string.Empty;
            return;
        }

        circuit.Layout = latest.Circuit.Clone();
        circuit.Label = OsPath.GetNameWithoutExtension(latest.Path);
    }

    private void AfterDriveChanged(Entity<ADTOperatingSystemComponent> ent, char letter)
    {
        if (GetFloppy(ent.Owner) is not { } floppy)
            return;

        if (letter != char.ToUpperInvariant(floppy.Comp.Drive))
            return;

        SyncCircuitFromFloppy(floppy);
    }
}
