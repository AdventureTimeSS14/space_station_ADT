using Content.Shared.ADT.Ghost;
using Robust.Server.GameObjects;
using Content.Server.Actions;
using Robust.Shared.Player;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;

namespace Content.Server.ADT.Ghost;

public sealed partial class GhostInfoSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ActionsSystem _action = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    /// <summary>
    /// Инициализирует систему и регистрирует обработчики событий, необходимых для синхронизации интерфейса Ghost Info с данными станции.
    /// </summary>
    /// <remarks>
    /// Подписывает локальные обработчики для событий компонента GhostInfo (открытие UI, инициализация карты, завершение компонента, действие GhostInfo)
    /// и для глобальных событий станции (переименование станции и изменение уровня тревоги).
    /// </remarks>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GhostInfoComponent, BoundUIOpenedEvent>(OnWindowOpen);
        SubscribeLocalEvent<GhostInfoComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GhostInfoComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<GhostInfoComponent, GhostInfoActionEvent>(GhostInfoAction);

        SubscribeLocalEvent<StationRenamedEvent>(OnStationRenamed);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
    }

    /// <summary>
    /// Обрабатывает открытие привязанного интерфейса GhostInfo: при совпадении ключа UI инициирует обновление всех PDA-интерфейсов станции.
    /// </summary>
    /// <param name="ent">Сущность с компонентом GhostInfo, для которой был открыт интерфейс.</param>
    /// <param name="args">Событие открытия UI; метод реагирует только если <c>args.UiKey</c> совпадает с ключом GhostInfo.</param>
    private void OnWindowOpen(Entity<GhostInfoComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!GhostInfoUiKey.Key.Equals(args.UiKey))
            return;

        UpdateAllPdaUisOnStation();
    }

    /// <summary>
    /// Обрабатывает инициализацию карты для сущности с компонентом GhostInfo: добавляет действие, связанное с компонентом, к сущности.
    /// </summary>
    /// <param name="uid">Идентификатор сущности, для которой инициализируется карта.</param>
    /// <param name="component">Компонент GhostInfo владельца сущности.</param>
    /// <param name="args">Аргументы события инициализации карты (не используются напрямую).</param>
    private void OnMapInit(EntityUid uid, GhostInfoComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.ActionEntity, component.Action);
    }

    /// <summary>
    /// Обрабатывает отключение <see cref="GhostInfoComponent"/> и удаляет связанное действие с сущности.
    /// </summary>
    /// <remarks>
    /// Вызывается при событии ComponentShutdown для данного компонента; удаление предотвращает наличие висящих действий после удаления компонента.
    /// </remarks>
    private void OnShutdown(EntityUid uid, GhostInfoComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.ActionEntity);
    }

    /// <summary>
    /// Обрабатывает событие действия GhostInfo: открывает интерфейс GhostInfo для владельца актёра и помечает событие как обработанное.
    /// </summary>
    /// <param name="uid">Идентификатор сущности, вызвавшей действие.</param>
    /// <param name="comp">Компонент GhostInfo связанный с сущностью.</param>
    /// <param name="args">Событие действия; если уже обработано — метод выйдет без действий. После успешного открытия UI событие помечается как обработанное.</param>
    private void GhostInfoAction(EntityUid uid, GhostInfoComponent comp, GhostInfoActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _uiSystem.TryOpenUi(uid, GhostInfoUiKey.Key, actor.Owner);

        args.Handled = true;
    }

    /// <summary>
    /// Обновляет все интерфейсы PDA с информацией о призраках, когда станция была переименована.
    /// </summary>
    private void OnStationRenamed(StationRenamedEvent ev)
    {
        UpdateAllPdaUisOnStation();
    }

    /// <summary>
    /// Обрабатывает событие изменения уровня тревоги и инициирует обновление всех GhostInfo UI на станции.
    /// </summary>
    /// <param name="args">Данные события изменения уровня тревоги (не используются напрямую в обработчике).</param>
    private void OnAlertLevelChanged(AlertLevelChangedEvent args)
    {
        UpdateAllPdaUisOnStation();
    }

    /// <summary>
    /// Обновляет состояние пользовательских интерфейсов GhostInfo (PDA) для всех сущностей станции.
    /// </summary>
    /// <remarks>
    /// Проходит по всем сущностям с компонентом <see cref="GhostInfoComponent"/> и вызывает <see cref="UpdateWindow"/> для каждой, чтобы синхронизировать отображаемую информацию станции и уровень тревоги.
    /// </remarks>
    private void UpdateAllPdaUisOnStation()
    {
        var query = AllEntityQuery<GhostInfoComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            UpdateWindow(ent, comp);
        }
    }

    /// <summary>
    /// Обновляет состояние UI GhostInfo для указанной сущности: получает/обновляет название станции, уровень тревоги и цвет, затем отправляет их в UI.
    /// </summary>
    /// <param name="uid">Идентификатор сущности с компонентом <see cref="GhostInfoComponent"/>. Если <paramref name="component"/> не передан, метод попытается его разрешить; при неудаче ничего не делает.</param>
    /// <param name="component">Опциональный экземпляр <see cref="GhostInfoComponent"/> для оптимизации вызова (может быть <c>null</c>).</param>
    public void UpdateWindow(EntityUid uid, GhostInfoComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!_uiSystem.HasUi(uid, GhostInfoUiKey.Key))
            return;
        var aIName = _entity.GetComponent<MetaDataComponent>(uid).EntityName;

        UpdateStationName(uid, component);
        UpdateAlertLevel(uid, component);

        var state = new GhostInfoUpdateState(
            component.StationName,
            component.StationAlertLevel,
            component.StationAlertColor
            );

        _uiSystem.SetUiState(uid, GhostInfoUiKey.Key, state);
    }

    /// <summary>
    /// Обновляет свойство <see cref="GhostInfoComponent.StationName"/> компонента по текущей станции, которой принадлежит сущность.
    /// </summary>
    /// <param name="uid">Идентификатор сущности, для которой определяется владеющая станция.</param>
    /// <param name="component">Компонент GhostInfo, у которого устанавливается StationName.</param>
    private void UpdateStationName(EntityUid uid, GhostInfoComponent component)
    {
        var station = _station.GetOwningStation(uid);
        component.StationName = station is null ? null : Name(station.Value);
    }

    /// <summary>
    /// Обновляет уровень тревоги и соответствующий цвет в переданном <see cref="GhostInfoComponent"/>,
    /// исходя из текущего состояния станции, которой принадлежит сущность <paramref name="uid"/>.
    /// </summary>
    /// <param name="uid">Идентификатор сущности, используемый для определения её станции-владельца.</param>
    /// <param name="component">Компонент GhostInfo, в котором будут обновлены поля StationAlertLevel и при наличии StationAlertColor.</param>
    private void UpdateAlertLevel(EntityUid uid, GhostInfoComponent component)
    {
        var station = _station.GetOwningStation(uid);
        if (!TryComp(station, out AlertLevelComponent? alertComp) ||
        alertComp.AlertLevels == null)
            return;
        component.StationAlertLevel = alertComp.CurrentLevel;
        if (alertComp.AlertLevels.Levels.TryGetValue(alertComp.CurrentLevel, out var details))
            component.StationAlertColor = details.Color;
    }
}
