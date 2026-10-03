// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Server.Traits;
using Content.Shared.ADT.Addiction;
using Content.Shared.ADT.Body.Allergies;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Addiction;

/// <summary>
/// Система зависимости: ловит приём доз (GetReagentEffectsEvent), копит уровень привыкания,
/// при долгом воздержании запускает ломку и рейзит AddictionSymptomsChangedEvent,
/// чтобы симптомы применила AddictionSymptomsSystem.
/// Компонент есть у всех игроков со спавна, канал зависимости появляется
/// при первом употреблении (подсесть может каждый), трайты дают стартовую
/// неизлечимую зависимость с высоким уровнем.
/// </summary>
public sealed partial class AddictionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddictionComponent, GetReagentEffectsEvent>(OnGetReagentEffects);
        // After TraitSystem: EnsureComp не должен перебить каналы, добавленные трайтом
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete, after: [typeof(TraitSystem)]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AddictionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateAddiction(uid, comp, frameTime);
        }
    }

    private void UpdateAddiction(EntityUid uid, AddictionComponent comp, float frameTime)
    {
        var dead = _mobState.IsDead(uid);

        foreach (var channel in comp.Channels)
            UpdateChannel(uid, comp, channel, frameTime, dead);
    }

    /// <summary>
    /// Обновляет один канал: инициализация рантайм-каналов, спад привыкания, ломка.
    /// </summary>
    private void UpdateChannel(EntityUid uid, AddictionComponent comp, AddictionChannel channel, float frameTime, bool dead)
    {
        InitRuntimeChannel(comp, channel);

        // Привыкание медленно спадает само. Трайтовые зависимости неизлечимы - не спадают.
        if (!channel.Permanent)
            channel.Level = MathF.Max(0f, channel.Level - comp.DecayRate * frameTime);

        var timeSinceDose = _timing.CurTime - channel.LastDoseTime;

        // Доза была недавно - ломки нет
        if (timeSinceDose < comp.WithdrawalDelay)
        {
            if (channel.InWithdrawal)
                StopWithdrawal(uid, channel);

            WarnNicotineCraving(uid, comp, channel, timeSinceDose, dead);
            UpdateNicotineAlert(uid, comp, channel, dead);
            return;
        }

        // Уровень упал ниже порога - зависимость отпустила
        if (channel.Level < comp.Threshold)
        {
            if (channel.WasAddicted)
            {
                channel.WasAddicted = false;
                if (!dead)
                    _popup.PopupEntity(Loc.GetString($"addiction-cured-{KindLoc(channel.Kind)}"), uid, uid, PopupType.Medium);
            }

            if (channel.InWithdrawal)
                StopWithdrawal(uid, channel);

            UpdateNicotineAlert(uid, comp, channel, dead);
            return;
        }

        if (dead)
        {
            UpdateNicotineAlert(uid, comp, channel, dead);
            return;
        }

        // Ломка: тяжесть сразу равна стадии зависимости (1 - лёгкая, 2 - средняя, 3 - тяжёлая).
        // Никотин исключение: даже при тяжёлой зависимости ломка нарастает по времени без сигареты.
        // Стадия может смягчиться во время ломки (лечение детоксином, долгое воздержание) -
        // тогда симптомы пересчитываются.
        var stage = WithdrawalStage(comp, channel, timeSinceDose);

        if (!channel.InWithdrawal || channel.Stage != stage)
        {
            // Новая ступень никотиновой ломки сразу говорит о себе, не дожидаясь следующего интервала.
            if (channel.InWithdrawal && stage > channel.Stage)
                channel.NextPopupTime = TimeSpan.Zero;

            channel.InWithdrawal = true;
            channel.Stage = stage;
            RaiseSymptomsChanged(uid);
        }

        if (_timing.CurTime >= channel.NextPopupTime)
        {
            var interval = channel.Kind == AddictionKind.Nicotine ? comp.NicotinePopupInterval : comp.PopupInterval;
            channel.NextPopupTime = _timing.CurTime + interval;
            _popup.PopupEntity(WithdrawalPopup(comp, channel, stage), uid, uid, WithdrawalPopupType(channel.Kind, stage));
            TryNicotineCough(uid, comp, channel, stage);
        }

        TryNicotineSevereCough(uid, comp, channel, stage);

        UpdateNicotineAlert(uid, comp, channel, dead);
    }

    /// <summary>
    /// Стадия ломки. У никотина не прыгает сразу к стадии зависимости:
    /// сначала тяга, потом муть, и только потом дрожь со слабостью.
    /// Выше стадии самой зависимости не поднимается.
    /// </summary>
    private static int WithdrawalStage(AddictionComponent comp, AddictionChannel channel, TimeSpan timeSinceDose)
    {
        var cap = AddictionStage.FromLevel(channel.Level);
        if (channel.Kind != AddictionKind.Nicotine)
            return cap;

        var overdue = timeSinceDose - comp.WithdrawalDelay;
        var byTime = overdue >= comp.NicotineStage3After ? 3
            : overdue >= comp.NicotineStage2After ? 2
            : 1;

        return Math.Min(cap, byTime);
    }

    /// <summary>
    /// Заранее говорит никотинщику, что таймер на иконке подходит к концу.
    /// </summary>
    private void WarnNicotineCraving(EntityUid uid, AddictionComponent comp, AddictionChannel channel, TimeSpan timeSinceDose, bool dead)
    {
        if (dead || channel.Kind != AddictionKind.Nicotine || channel.CravingWarned)
            return;

        if (channel.Level < comp.Threshold)
            return;

        if (timeSinceDose < comp.WithdrawalDelay * comp.NicotineCravingWarning)
            return;

        channel.CravingWarned = true;
        var idx = _random.Next(Math.Max(1, comp.NicotineCravingPopupVariants));
        _popup.PopupEntity(Loc.GetString($"addiction-craving-nicotine-{idx}"), uid, uid, PopupType.Medium);
    }

    private string WithdrawalPopup(AddictionComponent comp, AddictionChannel channel, int stage)
    {
        var locStage = stage - 1;
        if (channel.Kind != AddictionKind.Nicotine)
            return Loc.GetString($"addiction-withdrawal-{KindLoc(channel.Kind)}-{locStage}");

        var count = Math.Max(1, comp.NicotinePopupVariants);
        var idx = _random.Next(count);
        if (count > 1 && idx == channel.LastPopupIndex)
            idx = (idx + 1) % count;

        channel.LastPopupIndex = idx;
        return Loc.GetString($"addiction-withdrawal-nicotine-{locStage}-{idx}");
    }

    private static PopupType WithdrawalPopupType(AddictionKind kind, int stage)
    {
        if (kind != AddictionKind.Nicotine)
            return PopupType.Small;

        return stage switch
        {
            >= 3 => PopupType.LargeCaution,
            2 => PopupType.MediumCaution,
            _ => PopupType.Medium,
        };
    }

    private void TryNicotineCough(EntityUid uid, AddictionComponent comp, AddictionChannel channel, int stage)
    {
        if (channel.Kind != AddictionKind.Nicotine || stage < 1)
            return;

        if (stage >= 3)
            return;

        var chance = stage >= 2 ? comp.NicotineSevereCoughChance : comp.NicotineCoughChance;
        if (!_random.Prob(chance))
            return;

        _chat.TryEmoteWithChat(uid, comp.CoughEmote);
    }

    /// <summary>
    /// Тяжёлая никотиновая ломка: кашель раз в NicotineSevereCoughInterval, первый — сразу при входе в стадию.
    /// </summary>
    private void TryNicotineSevereCough(EntityUid uid, AddictionComponent comp, AddictionChannel channel, int stage)
    {
        if (channel.Kind != AddictionKind.Nicotine || stage < 3)
        {
            if (channel.NextCoughTime != TimeSpan.Zero)
                channel.NextCoughTime = TimeSpan.Zero;
            return;
        }

        if (channel.NextCoughTime != TimeSpan.Zero && _timing.CurTime < channel.NextCoughTime)
            return;

        channel.NextCoughTime = _timing.CurTime + comp.NicotineSevereCoughInterval;
        _chat.TryEmoteWithChat(uid, comp.CoughEmote);
    }

    /// <summary>
    /// Иконка справа: горящая сигарета с таймером, пока доза держится, и всё более мёртвая картинка в ломке.
    /// </summary>
    private void UpdateNicotineAlert(EntityUid uid, AddictionComponent comp, AddictionChannel channel, bool dead)
    {
        if (channel.Kind != AddictionKind.Nicotine)
            return;

        if (dead || channel.Level < comp.Threshold)
        {
            _alerts.ClearAlert(uid, comp.NicotineAlert);
            return;
        }

        short severity;
        (TimeSpan, TimeSpan)? cooldown = null;

        if (!channel.InWithdrawal)
        {
            severity = 0;
            cooldown = (channel.LastDoseTime, channel.LastDoseTime + comp.WithdrawalDelay);
        }
        else if (channel.Stage <= 1)
        {
            severity = 1;
        }
        else
        {
            severity = 2;
        }

        _alerts.ShowAlert(uid, comp.NicotineAlert, severity, cooldown, showCooldown: cooldown != null);
    }

    /// <summary>
    /// Инициализирует каналы, добавленные в рантайме (трайт-эффект после ComponentInit):
    /// без времени последней дозы ломка началась бы мгновенно, а WasAddicted
    /// нужен для корректных поп-апов подсадки и выздоровления.
    /// </summary>
    private void InitRuntimeChannel(AddictionComponent comp, AddictionChannel channel)
    {
        if (channel.LastDoseTime != TimeSpan.Zero)
            return;

        channel.LastDoseTime = _timing.CurTime;
        if (channel.Level >= comp.Threshold)
            channel.WasAddicted = true;
    }

    /// <summary>
    /// Снимает ломку: сбрасывает флаги и пересчитывает симптомы.
    /// </summary>
    private void StopWithdrawal(EntityUid uid, AddictionChannel channel)
    {
        channel.InWithdrawal = false;
        channel.Stage = 0;
        channel.NextCoughTime = TimeSpan.Zero;
        RaiseSymptomsChanged(uid);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Любой игрок может подсесть: компонент есть у всех с момента спавна
        var comp = EnsureComp<AddictionComponent>(args.Mob);

        // Рандомный трайт: выбрать случайный канал из ещё не выбранных (после TraitSystem
        // каналы конкретных трайтов уже в компоненте). Клиент блокирует выбор рандомного
        // при всех трёх конкретных, тут защита на случай обхода.
        if (!comp.RandomizeChannel)
            return;

        comp.RandomizeChannel = false;

        var taken = comp.Channels.Select(c => c.Kind).ToHashSet();
        var pool = new List<AddictionKind> { AddictionKind.Alcohol, AddictionKind.Nicotine, AddictionKind.Drug };
        pool.RemoveAll(taken.Contains);

        if (pool.Count == 0)
            return;

        var kind = _random.Pick(pool);
        comp.Channels.Add(new AddictionChannel
        {
            Kind = kind,
            Level = 100f,
            Permanent = true,
            LastDoseTime = _timing.CurTime,
            WasAddicted = true,
        });
    }

    private void OnGetReagentEffects(EntityUid uid, AddictionComponent comp, ref GetReagentEffectsEvent args)
    {
        var kind = GetKind(args.Reagent, comp);
        if (kind is not { } kindValue)
            return;

        var channel = comp.Channels.FirstOrDefault(c => c.Kind == kindValue);
        if (channel == null)
        {
            channel = new AddictionChannel { Kind = kindValue };
            comp.Channels.Add(channel);
        }

        ApplyDose(uid, comp, channel, comp.GainPerTick);
    }

    /// <summary>
    /// Учитывает дозу канала: растёт уровень, снимается ломка, фиксируется подсадка.
    /// Общий для обычного употребления (метаболизм) и передоза лекарств.
    /// </summary>
    public void ApplyDose(EntityUid uid, AddictionComponent comp, AddictionChannel channel, float amount)
    {
        channel.Level = MathF.Min(100f, channel.Level + amount);
        channel.LastDoseTime = _timing.CurTime;
        channel.NextPopupTime = TimeSpan.Zero;
        channel.CravingWarned = false;

        var popupType = channel.Kind == AddictionKind.Nicotine ? PopupType.Medium : PopupType.Small;

        // Доза снимает ломку (поп-ап только если ломка реально была)
        if (channel.InWithdrawal)
        {
            StopWithdrawal(uid, channel);
            _popup.PopupEntity(Loc.GetString($"addiction-dose-{KindLoc(channel.Kind)}"), uid, uid, popupType);
        }
        // Первое превышение порога - подсадка
        else if (!channel.WasAddicted && channel.Level >= comp.Threshold)
        {
            channel.WasAddicted = true;
            _popup.PopupEntity(Loc.GetString($"addiction-begin-{KindLoc(channel.Kind)}"), uid, uid, popupType);
        }

        if (channel.Kind == AddictionKind.Nicotine)
            UpdateNicotineAlert(uid, comp, channel, _mobState.IsDead(uid));
    }

    /// <summary>
    /// Определяет тип зависимости по рецепту: никотин по id, алкоголь по родительскому прототипу
    /// BaseAlcohol (в ADT все спиртные его наследуют), наркотики по группе реагента Narcotics.
    /// Настройки живут в компоненте.
    /// </summary>
    private AddictionKind? GetKind(ReagentId reagent, AddictionComponent comp)
    {
        if (reagent.Prototype == comp.NicotineReagent)
            return AddictionKind.Nicotine;

        if (!_proto.TryIndex(reagent.Prototype, out ReagentPrototype? proto))
            return null;

        if (proto.Parents != null && proto.Parents.Contains(comp.AlcoholParentId))
            return AddictionKind.Alcohol;

        if (proto.Group == comp.NarcoticGroup)
            return AddictionKind.Drug;

        return null;
    }

    /// <summary>
    /// Сообщает AddictionSymptomsSystem, что набор симптомов нужно пересчитать.
    /// </summary>
    private void RaiseSymptomsChanged(EntityUid uid)
    {
        var ev = new AddictionSymptomsChangedEvent(uid);
        RaiseLocalEvent(uid, ref ev);
    }

    private static string KindLoc(AddictionKind kind) => kind switch
    {
        AddictionKind.Alcohol => "alcohol",
        AddictionKind.Nicotine => "nicotine",
        AddictionKind.Drug => "drug",
        AddictionKind.Medicine => "medicine",
        AddictionKind.Omnizine => "omnizine",
        _ => "alcohol",
    };
}
