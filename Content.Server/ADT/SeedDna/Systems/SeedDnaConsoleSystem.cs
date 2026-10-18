using Content.Shared.ADT.SeedDna;
using Content.Shared.ADT.SeedDna.Components;
using Content.Shared.ADT.SeedDna.Systems;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.ADT.SeedDna.Systems;

[UsedImplicitly]
public sealed class SeedDnaConsoleSystem : SharedSeedDnaConsoleSystem
{
    [Dependency] private UserInterfaceSystem _userInterface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SeedDnaConsoleComponent, WriteToTargetSeedDataMessage>(OnWriteToTargetSeedDataMessage);

        SubscribeLocalEvent<SeedDnaConsoleComponent, ComponentStartup>(OnUpdateUserInterface);
        SubscribeLocalEvent<SeedDnaConsoleComponent, EntInsertedIntoContainerMessage>(OnUpdateUserInterface);
        SubscribeLocalEvent<SeedDnaConsoleComponent, EntRemovedFromContainerMessage>(OnUpdateUserInterface);
    }

    private void OnUpdateUserInterface(EntityUid uid, SeedDnaConsoleComponent component, EntityEventArgs args)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnWriteToTargetSeedDataMessage(EntityUid uid, SeedDnaConsoleComponent component, WriteToTargetSeedDataMessage args)
    {
        if (args.Target == TargetSeedData.Seed && component.SeedSlot.Item is { Valid: true } seedItem)
            RewriteSeedData(seedItem, args.SeedDataDto);
        else if (args.Target == TargetSeedData.DnaDisk && component.DnaDiskSlot.Item is { Valid: true } dnaDiskItem)
            RewriteDnaDiskData(dnaDiskItem, args.SeedDataDto);

        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, SeedDnaConsoleComponent component)
    {
        if (!component.Initialized)
            return;

        var (seedPresent, seedName, seedData) = ProcessSeedSlot(component);
        var (dnaDiskPresent, dnaDiskName, dnaDiskData) = ProcessDiskSlot(component);

        var newState = new SeedDnaConsoleBoundUserInterfaceState(
            seedPresent,
            seedName,
            seedData,
            dnaDiskPresent,
            dnaDiskName,
            dnaDiskData
        );
        _userInterface.SetUiState(uid, SeedDnaConsoleUiKey.Key, newState);
    }

    private (bool, string, SeedDataDto?) ProcessSeedSlot(SeedDnaConsoleComponent component)
    {
        return component.SeedSlot.Item is not { Valid: true } seedItem
            ? (false, string.Empty, null)
            : (true, EntityManager.GetComponent<MetaDataComponent>(seedItem).EntityName, ExtractSeedData(seedItem));
    }

    private void RewriteDnaDiskData(EntityUid dnaDisk, SeedDataDto dnaDiskDataDto)
    {
        EntityManager.GetComponent<DnaDiskComponent>(dnaDisk).SeedData = dnaDiskDataDto;
    }

    private (bool, string, SeedDataDto?) ProcessDiskSlot(SeedDnaConsoleComponent component)
    {
        return component.DnaDiskSlot.Item is not { Valid: true } diskItem
            ? (false, string.Empty, null)
            : (true, EntityManager.GetComponent<MetaDataComponent>(diskItem).EntityName, ExtractDiskData(diskItem));
    }

    private SeedDataDto? ExtractDiskData(EntityUid dnaDisk)
    {
        return EntityManager.GetComponent<DnaDiskComponent>(dnaDisk).SeedData;
    }
}
