// TODO: Запись рекордов. Работа сервера и клиента сапёра
using Content.Shared.ADT.Minesweeper;
using Content.Server.Administration;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.Administration;
using Content.Shared.Examine;
using Content.Shared.Instruments;
using Content.Shared.Instruments.UI;
using Content.Shared.Physics;
using Content.Shared.Popups;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Midi;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Server.Popups;
using Content.Shared.Abilities.Mime;
using Content.Shared.Actions;
using Content.Shared.Actions.Events;
using Content.Shared.Alert;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Maps;
using Content.Shared.Paper;
using Content.Shared.Physics;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Speech.Muting;


namespace Content.Server.ADT.Minesweeper;

public sealed partial class MinesweeperSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly UserInterfaceSystem _bui = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private string _userName = "";
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MinesweeperComponent, ActivateInWorldEvent>(OnInteractUI);
        SubscribeLocalEvent<MinesweeperComponent, BoundUIOpenedEvent>(OnBoundUiOpened);
        SubscribeLocalEvent<MinesweeperComponent, BoundUIClosedEvent>(OnBoundUiClosed);
        SubscribeLocalEvent<MinesweeperComponent, OnWinMessage>(OnWinMessageReceived);
    }

    private void OnInteractUI(EntityUid uid, MinesweeperComponent component, ActivateInWorldEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (component.LastOpenedBy != null)
        {
            var message = Loc.GetString("machine-already-in-use", ("machine", uid));
            _popupSystem.PopupEntity(message, uid, args.User);
            return;
        }
    }

    private void OnBoundUiOpened(EntityUid uid, MinesweeperComponent component, BoundUIOpenedEvent args)
    {
        // args.Session — это игрок, открывший интерфейс
        var actor = args.Actor;
        var name = _entityManager.GetComponent<MetaDataComponent>(actor);
        Logger.Info($"Игрок {name} открыл сапёра на объекте {ToPrettyString(uid)} name {name.EntityName}");

        _userName = name.EntityName;
        component.LastOpenedBy = name.EntityName;
        Dirty(uid, component);
    }

    private void OnBoundUiClosed(EntityUid uid, MinesweeperComponent component, BoundUIClosedEvent args)
    {
        component.LastOpenedBy = null;
        Dirty(uid, component);
    }

    private void OnWinMessageReceived(EntityUid uid, MinesweeperComponent component, OnWinMessage args)
    {
        Log.Debug($"OnWinMessageReceived принял сообщение {uid.ToString()}");
        // if (args.SenderSession.AttachedEntity is not { } uid)
        //     return;


        if (!_entityManager.TryGetComponent<MinesweeperComponent>(uid, out var comp))
            return;

        comp.LastOpenedBy = _userName;

        Log.Debug($"OnWinMessageReceived было Dirty(uid, comp); {_userName}");
        Dirty(uid, comp);
    }
}
