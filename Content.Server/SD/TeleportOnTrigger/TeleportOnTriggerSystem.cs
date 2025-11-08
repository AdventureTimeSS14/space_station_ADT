using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Robust.Server.GameObjects;
using Content.Shared.Implants.Components;
using Robust.Shared.Localization;
using Content.Server.Chat.Managers;
using Content.Shared.Nuke;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;

namespace Content.Server._SD.Implants;

public sealed class TeleportOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleportOnTriggerComponent, TriggerEvent>(OnImplantTriggered);
    }

    private void OnImplantTriggered(EntityUid uid, TeleportOnTriggerComponent component, TriggerEvent args)
    {
        if (args.Handled)
            return;

        // Получаем пользователя, в которого установлен имплант
        if (!TryComp<SubdermalImplantComponent>(uid, out var implant) ||
            implant.ImplantedEntity is not { } user)
        {
            return;
        }

        if (HasNukeDisk(user))
        {
            _popup.PopupEntity(Loc.GetString("emergency-teleport-has-nuke-disk"), user, user, PopupType.LargeCaution);
            args.Handled = true;
            return;
        }

        // Ищем маркер для телепортации по прототипу
        var markerPrototypeId = component.MarkerPrototype.ToString();
        var marker = FindTeleportMarker(markerPrototypeId);
        if (marker == null)
        {
            // Если маркер не найден, показываем сообщение и отменяем телепортацию
            _popup.PopupEntity(Loc.GetString("emergency-teleport-no-marker"), user, user, PopupType.LargeCaution);
            args.Handled = true;
            return;
        }

        // Телепортируем пользователя
        var markerXform = Transform(marker.Value);
        _transform.SetCoordinates(user, markerXform.Coordinates);

        // Если нужно убить пользователя и он еще жив
        if (component.KillOnTeleport && !_mobState.IsDead(user))
        {
            _mobState.ChangeMobState(user, MobState.Dead);
        }

        _popup.PopupEntity(Loc.GetString("emergency-teleport-success"), user, user, PopupType.LargeCaution);
        _chatManager.SendAdminAlert(Loc.GetString("admin-lifeline-tp", ("playerName", ToPrettyString(user))));
        args.Handled = true;
    }

    private bool HasNukeDisk(EntityUid user)
    {
        var diskQuery = EntityQueryEnumerator<NukeDiskComponent>();
        while (diskQuery.MoveNext(out var diskUid, out _))
        {
            var diskTransform = Transform(diskUid);

            var parent = diskTransform.ParentUid;
            while (parent.IsValid())
            {
                if (parent == user)
                {
                    return true;
                }
                parent = Transform(parent).ParentUid;
            }
        }

        return false;
    }

    private EntityUid? FindTeleportMarker(string markerPrototypeId)
    {
        var query = EntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out var uid, out var metadata))
        {
            if (metadata.EntityPrototype?.ID == markerPrototypeId)
                return uid;
        }
        return null;
    }
}
