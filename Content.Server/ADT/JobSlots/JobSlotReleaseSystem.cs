using System.Linq;
// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.MassMedia.Systems;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared.Atmos.Rotting;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Gibbing;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.JobSlots;

/// <summary>
/// ADT: освобождает слот должности для позднего входа, когда игрок окончательно потерял тело:
/// - тело сгнило (BeginRottingEvent, с этого момента дефибриллятор не работает);
/// - тело уничтожено/расчленено (BeingGibbedEvent);
/// - гостаут/дисконнект из ЖИВОГО тела (игрок не вернулся).
/// Слот освобождается ТОЛЬКО через <see cref="ReleaseDelay"/> после события: за это время
/// успевает отработать клонирование и отыграть ВрИО (acting head), чтобы не было дублей должности.
/// Паттерн возврата слота скопирован из CryostorageSystem: PlayerJobs -> TryAdjustJobSlot(+1, clamp) -> TryRemovePlayerJobs.
/// Об освободившейся должности публикуется новость в КПК (NewsSystem).
/// </summary>
public sealed partial class JobSlotReleaseSystem : EntitySystem
{
    /// <summary>
    /// Задержка между потерей тела и освобождением слота: гостаут из живого тела,
    /// начало гниения, гибб. Даёт время на клонирование и отыгрыш ВрИО.
    /// </summary>
    public static readonly TimeSpan ReleaseDelay = TimeSpan.FromMinutes(10);

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly NewsSystem _news = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;

    private sealed record JobSlotData(
        NetUserId UserId,
        EntityUid Station,
        ProtoId<JobPrototype> JobId,
        TimeSpan? GhostSince,
        TimeSpan? ReleaseAt);

    /// <summary>
    /// Тела игроков, за которыми мы следим: тело -> данные о должности.
    /// Запись может пережить удаление тела (гибб): тогда освобождение идёт по таймеру.
    /// </summary>
    private readonly Dictionary<EntityUid, JobSlotData> _tracked = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<PerishableComponent, BeginRottingEvent>(OnBeginRotting);
        SubscribeLocalEvent<PerishableComponent, BeingGibbedEvent>(OnBeingGibbed);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_tracked.Count == 0)
            return;

        var curTime = _timing.CurTime;

        foreach (var (mob, data) in _tracked.ToArray())
        {
            // Тело удалено (гибб или админ-удаление). Освобождаем по таймеру, если он был выставлен.
            if (!Exists(mob))
            {
                if (data.ReleaseAt != null && curTime >= data.ReleaseAt.Value)
                    ReleaseJobSlot(mob);
                else if (data.ReleaseAt == null)
                    _tracked.Remove(mob);
                continue;
            }

            // Крио-тела лежат на паузной карте - их слот уже вернул CryostorageSystem, не трогаем.
            if (MetaData(mob).EntityPaused)
                continue;

            // Игрок в теле (вернулся, оживлён) - отменяем все таймеры освобождения.
            if (HasComp<ActorComponent>(mob))
            {
                if (data.GhostSince != null || data.ReleaseAt != null)
                    _tracked[mob] = data with { GhostSince = null, ReleaseAt = null };
                continue;
            }

            // Ждём дедлайн освобождения (рот/гибб уже случились).
            if (data.ReleaseAt != null)
            {
                // Игрок успел получить новое тело (клон и т.п.): освобождение отменяем,
                // слот остаётся за ним (PlayerJobs не трогаем).
                if (_mind.TryGetMind(data.UserId, out var mind) &&
                    mind.Value.Comp.CurrentEntity is { } current &&
                    current != mob &&
                    !HasComp<GhostComponent>(current))
                {
                    _tracked.Remove(mob);
                    continue;
                }

                if (curTime >= data.ReleaseAt.Value)
                    ReleaseJobSlot(mob);
                continue;
            }

            // Мёртвое тело без игрока: слот освободит рот (BeginRottingEvent + задержка), ждём.
            if (TryComp<MobStateComponent>(mob, out var mobState) && _mobState.IsDead(mob, mobState))
                continue;

            // Живое тело без игрока = гостаут (или дисконнект). Засекаем время.
            if (data.GhostSince == null)
            {
                _tracked[mob] = data with { GhostSince = curTime };
                continue;
            }

            if (curTime - data.GhostSince.Value >= ReleaseDelay)
                ReleaseJobSlot(mob);
        }
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId == null)
            return;

        // Не удаляем старые записи этого игрока: если его прежнее тело сгниёт или он бросил его
        // гостаутом, слот СТАРОЙ должности должен освободиться отдельно от новой.
        _tracked[ev.Mob] = new JobSlotData(ev.Player.UserId, ev.Station, ev.JobId, null, null);
    }

    private void OnBeginRotting(Entity<PerishableComponent> ent, ref BeginRottingEvent args)
    {
        ScheduleRelease(ent);
    }

    private void OnBeingGibbed(Entity<PerishableComponent> ent, ref BeingGibbedEvent args)
    {
        ScheduleRelease(ent);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _tracked.Clear();
    }

    /// <summary>
    /// Ставит таймер освобождения слота (если ещё не стоял). Тело при гиббе скоро удалится,
    /// поэтому запись не трогаем - её обработает Update.
    /// </summary>
    private void ScheduleRelease(EntityUid mob)
    {
        if (!_tracked.TryGetValue(mob, out var data) || data.ReleaseAt != null)
            return;

        _tracked[mob] = data with { ReleaseAt = _timing.CurTime + ReleaseDelay };
    }

    /// <summary>
    /// Возвращает станции все слоты должностей, занятые игроком, и публикует новость в КПК.
    /// </summary>
    private void ReleaseJobSlot(EntityUid mob)
    {
        if (!_tracked.Remove(mob, out var data))
            return;

        if (!TryComp<StationJobsComponent>(data.Station, out var stationJobs))
            return;

        // Слот мог быть уже возвращён (крио) - тогда возвращать нечего.
        if (!_stationJobs.TryGetPlayerJobs(data.Station, data.UserId, out var jobs, stationJobs))
            return;

        // Возвращаем только слот должности ЭТОГО тела: у игрока может быть новая должность
        // (поздний вход), её слот не трогаем.
        if (jobs.Remove(data.JobId))
            _stationJobs.TryAdjustJobSlot(data.Station, data.JobId, 1, clamp: true);

        if (jobs.Count == 0)
            _stationJobs.TryRemovePlayerJobs(data.Station, data.UserId, stationJobs);

        PublishVacancyNews(data.Station, data.JobId);
    }

    private void PublishVacancyNews(EntityUid station, ProtoId<JobPrototype> job)
    {
        var jobName = _proto.Index(job).LocalizedName;

        _news.TryAddNews(
            station,
            Loc.GetString("adt-job-slot-release-news-title", ("job", jobName)),
            Loc.GetString("adt-job-slot-release-news-content", ("job", jobName), ("station", Name(station))),
            out _,
            author: Loc.GetString("adt-job-slot-release-news-author"));
    }
}
