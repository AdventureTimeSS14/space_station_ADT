// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.ADT.Traits;
using Content.Server.Chat.Systems;
using Content.Server.Popups;
using Content.Shared.ADT.Addiction;
using Content.Shared.ADT.Body.Allergies;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Addiction;

/// <summary>
/// Копит уровень привыкания от доз и запускает ломку при воздержании.
/// Конкретные зависимости не различает - всё лежит в AddictionPrototype.
/// </summary>
public sealed class AddictionSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private ChatSystem _chat = default!;

    private const float UpdateInterval = 1f;

    private float _accumulator;

    /// <summary>
    /// Null - реаген ничего не кормит.
    /// </summary>
    private readonly Dictionary<string, string?> _reagentKinds = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddictionComponent, GetReagentEffectsEvent>(OnGetReagentEffects);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete, after: [typeof(ADTTraitSystem)]);

        _proto.PrototypesReloaded += OnPrototypesReloaded;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _proto.PrototypesReloaded -= OnPrototypesReloaded;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        _reagentKinds.Clear();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _accumulator += frameTime;
        if (_accumulator < UpdateInterval)
            return;

        var elapsed = _accumulator;
        _accumulator = 0f;

        var query = EntityQueryEnumerator<AddictionComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var dead = _mobState.IsDead(uid);

            foreach (var channel in comp.Channels)
                UpdateChannel(uid, channel, elapsed, dead);
        }
    }

    private void UpdateChannel(EntityUid uid, AddictionChannel channel, float elapsed, bool dead)
    {
        if (!_proto.TryIndex(channel.Kind, out var proto))
            return;

        InitChannel(channel, proto);

        if (!channel.Permanent)
            channel.Level = MathF.Max(0f, channel.Level - proto.DecayPerSecond * elapsed);

        var timeSinceDose = _timing.CurTime - channel.LastDoseTime;

        if (timeSinceDose < proto.Withdrawal.Delay)
        {
            if (channel.InWithdrawal)
                StopWithdrawal(uid, channel);

            WarnCraving(uid, channel, proto, timeSinceDose, dead);
            UpdateAlert(uid, channel, proto, dead);
            return;
        }

        if (channel.Level < proto.CureThreshold)
        {
            if (channel.WasAddicted)
            {
                channel.WasAddicted = false;
                if (!dead && proto.CuredPopup is { } cured)
                    _popup.PopupEntity(Loc.GetString(cured), uid, uid, proto.LifecyclePopupType);
            }

            if (channel.InWithdrawal)
                StopWithdrawal(uid, channel);

            UpdateAlert(uid, channel, proto, dead);
            return;
        }

        if (dead)
        {
            UpdateAlert(uid, channel, proto, dead);
            return;
        }

        var stage = WithdrawalStage(proto, channel, timeSinceDose);
        var stageData = proto.GetStage(stage);

        if (!channel.InWithdrawal || channel.Stage != stage)
        {
            if (channel.InWithdrawal && stage > channel.Stage)
                channel.NextPopupTime = TimeSpan.Zero;

            channel.InWithdrawal = true;
            channel.Stage = stage;
            ResetEmoteTimers(channel, stageData);
            RaiseSymptomsChanged(uid);
        }

        if (stageData == null)
        {
            UpdateAlert(uid, channel, proto, dead);
            return;
        }

        if (_timing.CurTime >= channel.NextPopupTime)
        {
            channel.NextPopupTime = _timing.CurTime + stageData.PopupInterval;

            if (stageData.Popups.Count > 0)
                _popup.PopupEntity(PickPopup(stageData.Popups, channel), uid, uid, stageData.PopupType);

            TryPopupEmotes(uid, stageData);
        }

        TryIntervalEmotes(uid, channel, stageData);
        UpdateAlert(uid, channel, proto, dead);
    }

    private static int WithdrawalStage(AddictionPrototype proto, AddictionChannel channel, TimeSpan timeSinceDose)
    {
        var cap = proto.StageFromLevel(channel.Level);
        if (proto.Withdrawal.Ramp.Count == 0)
            return cap;

        var overdue = timeSinceDose - proto.Withdrawal.Delay;
        var byTime = 1;
        foreach (var step in proto.Withdrawal.Ramp)
        {
            if (overdue >= step.After && step.Stage > byTime)
                byTime = step.Stage;
        }

        return Math.Min(cap, byTime);
    }

    private void WarnCraving(EntityUid uid, AddictionChannel channel, AddictionPrototype proto, TimeSpan timeSinceDose, bool dead)
    {
        if (dead || channel.CravingWarned)
            return;

        var withdrawal = proto.Withdrawal;
        if (withdrawal.WarningAt is not { } at || withdrawal.WarningPopups.Count == 0)
            return;

        if (channel.Level < proto.CureThreshold)
            return;

        if (timeSinceDose < withdrawal.Delay * at)
            return;

        channel.CravingWarned = true;

        var text = Loc.GetString(_random.Pick(withdrawal.WarningPopups));
        _popup.PopupEntity(text, uid, uid, withdrawal.WarningPopupType);
    }

    private string PickPopup(List<LocId> popups, AddictionChannel channel)
    {
        if (popups.Count == 1)
            return Loc.GetString(popups[0]);

        var idx = _random.Next(popups.Count);
        if (idx == channel.LastPopupIndex)
            idx = (idx + 1) % popups.Count;

        channel.LastPopupIndex = idx;
        return Loc.GetString(popups[idx]);
    }

    private void TryPopupEmotes(EntityUid uid, AddictionStageData stageData)
    {
        foreach (var emote in stageData.Emotes)
        {
            if (emote.PopupChance <= 0f || !_random.Prob(emote.PopupChance))
                continue;

            _chat.TryEmoteWithChat(uid, emote.Emote);
        }
    }

    private void TryIntervalEmotes(EntityUid uid, AddictionChannel channel, AddictionStageData stageData)
    {
        for (var i = 0; i < stageData.Emotes.Count; i++)
        {
            var emote = stageData.Emotes[i];
            if (emote.Interval is not { } interval)
                continue;

            if (i >= channel.NextEmoteTimes.Count)
                continue;

            if (channel.NextEmoteTimes[i] != TimeSpan.Zero && _timing.CurTime < channel.NextEmoteTimes[i])
                continue;

            channel.NextEmoteTimes[i] = _timing.CurTime + interval;
            _chat.TryEmoteWithChat(uid, emote.Emote);
        }
    }

    private static void ResetEmoteTimers(AddictionChannel channel, AddictionStageData? stageData)
    {
        channel.NextEmoteTimes.Clear();

        if (stageData == null)
            return;

        for (var i = 0; i < stageData.Emotes.Count; i++)
            channel.NextEmoteTimes.Add(TimeSpan.Zero);
    }

    private void UpdateAlert(EntityUid uid, AddictionChannel channel, AddictionPrototype proto, bool dead)
    {
        if (proto.Alert is not { } alert)
            return;

        if (dead || channel.Level < proto.CureThreshold)
        {
            _alerts.ClearAlert(uid, alert);
            return;
        }

        short severity;
        (TimeSpan, TimeSpan)? cooldown = null;

        if (!channel.InWithdrawal)
        {
            severity = 0;
            if (proto.AlertCooldown)
                cooldown = (channel.LastDoseTime, channel.LastDoseTime + proto.Withdrawal.Delay);
        }
        else
        {
            severity = (short) Math.Clamp(channel.Stage, 1, proto.MaxAlertSeverity);
        }

        _alerts.ShowAlert(uid, alert, severity, cooldown, showCooldown: cooldown != null);
    }

    private void InitChannel(AddictionChannel channel, AddictionPrototype proto)
    {
        if (channel.Initialized)
            return;

        channel.Initialized = true;

        if (channel.LastDoseTime == TimeSpan.Zero)
            channel.LastDoseTime = _timing.CurTime;

        if (channel.Level >= proto.CureThreshold)
            channel.WasAddicted = true;
    }

    private void StopWithdrawal(EntityUid uid, AddictionChannel channel)
    {
        channel.InWithdrawal = false;
        channel.Stage = 0;
        channel.NextEmoteTimes.Clear();
        RaiseSymptomsChanged(uid);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        var comp = EnsureComp<AddictionComponent>(args.Mob);

        // После ADTTraitSystem: каналы конкретных трайтов уже в компоненте.
        if (comp.PendingRandomChannel is not { } pending)
            return;

        var pool = comp.PendingRandomPool;
        comp.PendingRandomChannel = null;
        comp.PendingRandomPool = null;

        if (pool == null)
            return;

        var taken = comp.Channels.Select(c => c.Kind).ToHashSet();
        var choices = pool.Where(kind => !taken.Contains(kind) && _proto.HasIndex(kind)).ToList();

        if (choices.Count == 0)
            return;

        pending.Kind = _random.Pick(choices);
        comp.Channels.Add(pending);
    }

    private void OnGetReagentEffects(EntityUid uid, AddictionComponent comp, ref GetReagentEffectsEvent args)
    {
        if (ResolveKind(args.Reagent) is not { } kind)
            return;

        if (!_proto.TryIndex<AddictionPrototype>(kind, out var proto))
            return;

        ApplyDose(uid, EnsureChannel(comp, kind), proto, proto.GainPerDose);
    }

    private string? ResolveKind(ReagentId reagent)
    {
        if (_reagentKinds.TryGetValue(reagent.Prototype, out var cached))
            return cached;

        _proto.TryIndex(reagent.Prototype, out ReagentPrototype? reagentProto);

        string? found = null;

        // Реаген может подойти нескольким зависимостям - решает Priority.
        foreach (var proto in _proto.EnumeratePrototypes<AddictionPrototype>()
                     .OrderByDescending(p => p.Priority)
                     .ThenBy(p => p.ID))
        {
            if (!proto.Sources.Any(source => source.Matches(reagent, reagentProto)))
                continue;

            found = proto.ID;
            break;
        }

        _reagentKinds[reagent.Prototype] = found;
        return found;
    }

    public AddictionChannel EnsureChannel(AddictionComponent comp, ProtoId<AddictionPrototype> kind)
    {
        foreach (var existing in comp.Channels)
        {
            if (existing.Kind == kind)
                return existing;
        }

        var channel = new AddictionChannel { Kind = kind, LastDoseTime = _timing.CurTime };
        comp.Channels.Add(channel);
        return channel;
    }

    public void ApplyDose(EntityUid uid, AddictionChannel channel, AddictionPrototype proto, float amount)
    {
        channel.Initialized = true;
        channel.Level = MathF.Min(proto.MaxLevel, channel.Level + amount);
        channel.LastDoseTime = _timing.CurTime;
        channel.NextPopupTime = TimeSpan.Zero;
        channel.CravingWarned = false;

        if (channel.InWithdrawal)
        {
            StopWithdrawal(uid, channel);

            if (proto.DosePopup is { } dose)
                _popup.PopupEntity(Loc.GetString(dose), uid, uid, proto.LifecyclePopupType);
        }
        else if (!channel.WasAddicted && channel.Level >= proto.CureThreshold)
        {
            channel.WasAddicted = true;

            if (proto.BeginPopup is { } begin)
                _popup.PopupEntity(Loc.GetString(begin), uid, uid, proto.LifecyclePopupType);
        }

        UpdateAlert(uid, channel, proto, _mobState.IsDead(uid));
    }

    /// <summary>
    /// Только сформировавшиеся зависимости.
    /// </summary>
    public List<AddictionInfo>? GetAddictionInfo(EntityUid uid)
    {
        if (!TryComp<AddictionComponent>(uid, out var comp))
            return null;

        List<AddictionInfo>? result = null;

        foreach (var channel in comp.Channels)
        {
            if (!channel.WasAddicted || !_proto.TryIndex(channel.Kind, out var proto))
                continue;

            result ??= new List<AddictionInfo>();
            result.Add(new AddictionInfo(channel.Kind.Id, proto.StageFromLevel(channel.Level), channel.Permanent));
        }

        return result;
    }

    private void RaiseSymptomsChanged(EntityUid uid)
    {
        var ev = new AddictionSymptomsChangedEvent(uid);
        RaiseLocalEvent(uid, ref ev);
    }
}
