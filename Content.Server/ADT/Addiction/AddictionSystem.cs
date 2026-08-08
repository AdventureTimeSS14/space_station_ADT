// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Popups;
using Content.Server.Traits;
using Content.Shared.ADT.Addiction;
using Content.Shared.ADT.Body.Allergies;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
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

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddictionComponent, GetReagentEffectsEvent>(OnGetReagentEffects);
        SubscribeLocalEvent<AddictionComponent, ComponentInit>(OnComponentInit);
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

    private void OnComponentInit(EntityUid uid, AddictionComponent comp, ComponentInit args)
    {
        // Каналы из трайта приходят с LastDoseTime = 0, иначе ломка началась бы мгновенно.
        // Уровень выше порога означает, что зависимость уже есть: без WasAddicted
        // лечение не показало бы поп-ап выздоровления, а доза до лечения - поп-ап подсадки.
        foreach (var channel in comp.Channels)
        {
            if (channel.LastDoseTime == TimeSpan.Zero)
                channel.LastDoseTime = _timing.CurTime;

            if (channel.Level >= comp.Threshold)
                channel.WasAddicted = true;
        }
    }

    private void UpdateAddiction(EntityUid uid, AddictionComponent comp, float frameTime)
    {
        var dead = _mobState.IsDead(uid);

        foreach (var channel in comp.Channels)
        {
            // Каналы, добавленные в рантайме (трайт-эффект после ComponentInit),
            // не прошли инициализацию: без времени дозы ломка началась бы мгновенно.
            if (channel.LastDoseTime == TimeSpan.Zero)
            {
                channel.LastDoseTime = _timing.CurTime;
                if (channel.Level >= comp.Threshold)
                    channel.WasAddicted = true;
            }

            // Привыкание медленно спадает само. Трайтовые зависимости неизлечимы - не спадают.
            if (!channel.Permanent)
                channel.Level = MathF.Max(0f, channel.Level - comp.DecayRate * frameTime);

            var timeSinceDose = _timing.CurTime - channel.LastDoseTime;

            // Доза была недавно - ломки нет
            if (timeSinceDose < comp.WithdrawalDelay)
            {
                if (channel.InWithdrawal)
                {
                    channel.InWithdrawal = false;
                    channel.Stage = 0;
                    RaiseSymptomsChanged(uid);
                }
                continue;
            }

            // Уровень упал ниже порога - зависимость отпустила
            if (channel.Level < comp.Threshold)
            {
                if (channel.WasAddicted)
                {
                    channel.WasAddicted = false;
                    if (!dead)
                        _popup.PopupEntity(Loc.GetString($"addiction-cured-{KindLoc(channel.Kind)}"), uid, uid);
                }

                if (channel.InWithdrawal)
                {
                    channel.InWithdrawal = false;
                    channel.Stage = 0;
                    RaiseSymptomsChanged(uid);
                }
                continue;
            }

            if (dead)
                continue;

            // Ломка: тяжесть сразу равна стадии зависимости (1 - лёгкая, 2 - средняя, 3 - тяжёлая).
            // Стадия может смягчиться во время ломки (лечение детоксином, долгое воздержание) -
            // тогда симптомы пересчитываются.
            var stage = AddictionStage.FromLevel(channel.Level);

            if (!channel.InWithdrawal || channel.Stage != stage)
            {
                channel.InWithdrawal = true;
                channel.Stage = stage;
                RaiseSymptomsChanged(uid);
            }

            if (_timing.CurTime >= channel.NextPopupTime)
            {
                channel.NextPopupTime = _timing.CurTime + comp.PopupInterval;
                // Ключи поп-апов индексируются 0/1/2 (лёгкая/средняя/тяжёлая)
                _popup.PopupEntity(Loc.GetString($"addiction-withdrawal-{KindLoc(channel.Kind)}-{stage - 1}"), uid, uid);
            }
        }
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

        channel.Level = MathF.Min(100f, channel.Level + comp.GainPerTick);
        channel.LastDoseTime = _timing.CurTime;
        channel.NextPopupTime = TimeSpan.Zero;

        // Доза снимает ломку (поп-ап только если ломка реально была)
        if (channel.InWithdrawal)
        {
            channel.InWithdrawal = false;
            channel.Stage = 0;
            RaiseSymptomsChanged(uid);
            _popup.PopupEntity(Loc.GetString($"addiction-dose-{KindLoc(kindValue)}"), uid, uid);
        }
        // Первое превышение порога - подсадка
        else if (!channel.WasAddicted && channel.Level >= comp.Threshold)
        {
            channel.WasAddicted = true;
            _popup.PopupEntity(Loc.GetString($"addiction-begin-{KindLoc(kindValue)}"), uid, uid);
        }
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
        _ => "alcohol",
    };
}
