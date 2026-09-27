using Content.Shared.Access.Systems;
using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.InconnuOS.Components;

namespace Content.Server.ADT.InconnuOS;

public sealed partial class ADTOsSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;

    partial void InitializeUi()
    {
        SubscribeLocalEvent<ADTOperatingSystemComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ADTOperatingSystemComponent, BoundUIClosedEvent>(OnUiClosed);

        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsFileWriteMessage>(OnFileWrite);
        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsFileDeleteMessage>(OnFileDelete);
        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsFileMoveMessage>(OnFileMove);
        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsCreateDirectoryMessage>(OnCreateDirectory);
        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsSettingsMessage>(OnSettings);
        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsPowerMessage>(OnPower);
    }

    private void OnUiOpened(Entity<ADTOperatingSystemComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not ADTComputerUiKey)
            return;

        var comp = ent.Comp;

        PowerOn(ent);

        comp.UiOpen = true;
        comp.SessionUser = GetUserName(args.Actor);

        UpdateUiState(ent);
    }

    private void OnUiClosed(Entity<ADTOperatingSystemComponent> ent, ref BoundUIClosedEvent args)
    {
        if (args.UiKey is not ADTComputerUiKey)
            return;

        ent.Comp.UiOpen = _ui.IsUiOpen(ent.Owner, ADTComputerUiKey.Key);
    }

    private string GetUserName(EntityUid actor)
    {
        if (!_idCard.TryFindIdCard(actor, out var card))
            return Loc.GetString("os-user-guest");

        if (string.IsNullOrWhiteSpace(card.Comp.FullName))
            return Loc.GetString("os-user-guest");

        return card.Comp.FullName;
    }

    private void OnFileWrite(Entity<ADTOperatingSystemComponent> ent, ref ADTOsFileWriteMessage args)
    {
        if (!CanOperate(ent))
            return;

        if (!TryWriteFile(ent, args.Path, args.Kind, args.Text, args.Circuit, out var error, out var detail))
        {
            Deny(ent, args.Actor, error, detail);
            return;
        }

        UpdateUiState(ent);
    }

    private void OnFileDelete(Entity<ADTOperatingSystemComponent> ent, ref ADTOsFileDeleteMessage args)
    {
        if (!CanOperate(ent))
            return;

        if (!TryDeleteFile(ent, args.Path, true, out var error, out var detail))
        {
            Deny(ent, args.Actor, error, detail);
            return;
        }

        UpdateUiState(ent);
    }

    private void OnFileMove(Entity<ADTOperatingSystemComponent> ent, ref ADTOsFileMoveMessage args)
    {
        if (!CanOperate(ent))
            return;

        if (!TryMoveFile(ent, args.From, args.To, args.Copy, out var error, out var detail))
        {
            Deny(ent, args.Actor, error, detail);
            return;
        }

        UpdateUiState(ent);
    }

    private void OnCreateDirectory(Entity<ADTOperatingSystemComponent> ent, ref ADTOsCreateDirectoryMessage args)
    {
        if (!CanOperate(ent))
            return;

        if (!TryMakeDirectory(ent, args.Path, out var error, out var detail))
        {
            Deny(ent, args.Actor, error, detail);
            return;
        }

        UpdateUiState(ent);
    }

    private void OnSettings(Entity<ADTOperatingSystemComponent> ent, ref ADTOsSettingsMessage args)
    {
        if (!CanOperate(ent))
            return;

        var settings = args.Settings.Clone();

        settings.Volume = Math.Clamp(settings.Volume, 0f, 1f);

        if (!Enum.IsDefined(settings.Wallpaper))
            settings.Wallpaper = OsWallpaper.Depths;

        ent.Comp.Settings = settings;

        UpdateUiState(ent);
    }

    private void OnPower(Entity<ADTOperatingSystemComponent> ent, ref ADTOsPowerMessage args)
    {
        if (args.Action == OsPowerAction.Reboot)
        {
            Reboot(ent);
            return;
        }

        PowerOff(ent);
    }

    private static bool CanOperate(Entity<ADTOperatingSystemComponent> ent)
    {
        return !ent.Comp.Crashed;
    }

    private void Deny(Entity<ADTOperatingSystemComponent> ent, EntityUid actor, OsValidationError error, string detail)
    {
        if (!actor.IsValid())
            return;

        _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key, new ADTOsErrorMessage(error, detail), actor);
    }

    public void UpdateUiState(Entity<ADTOperatingSystemComponent> ent)
    {
        var comp = ent.Comp;

        if (!comp.UiOpen)
            return;

        var state = new ADTOsBuiState(
            comp.MachineName,
            comp.SessionUser,
            BuildDrives(ent),
            comp.Apps.ToArray(),
            comp.Settings,
            GetLimits(comp),
            comp.BootedAt,
            comp.Activated,
            comp.Crashed);

        _ui.SetUiState(ent.Owner, ADTComputerUiKey.Key, state);
    }
}
