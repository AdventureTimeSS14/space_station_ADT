using Content.Server.ADT.Dissociative.Components;
using Content.Shared.ADT.Dissociative;
using Content.Shared.ADT.TTS;
using Content.Shared.Body;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Dissociative;

public sealed partial class DissociativeIdentitySystem
{
    [Dependency] private MobStateSystem _mobState = default!;

    private bool _swapping;

    private void InitializeFront()
    {
        SubscribeLocalEvent<DissociativeAlterComponent, DissociativeFrontActionEvent>(OnFrontAction);
        SubscribeLocalEvent<DissociativeIdentityComponent, DissociativeReleaseActionEvent>(OnReleaseAction);
        SubscribeLocalEvent<DissociativeIdentityComponent, DissociativeGiveControlActionEvent>(OnGiveControlAction);
        SubscribeLocalEvent<DissociativeIdentityComponent, MobStateChangedEvent>(OnHostStateChanged);
        SubscribeLocalEvent<DissociativeIdentityComponent, MindRemovedMessage>(OnHostMindRemoved);
        SubscribeLocalEvent<DissociativeIdentityComponent, ExaminedEvent>(OnHostExamined);
    }

    public void OnAntipsychotic(Entity<DissociativeIdentityComponent> ent)
    {
        ent.Comp.LastAntipsychotic = _timing.CurTime;

        if (ent.Comp.Fronting)
            ent.Comp.FrontEnd = _timing.CurTime;
    }

    public void ExtendFront(Entity<DissociativeIdentityComponent> ent, TimeSpan time)
    {
        if (!ent.Comp.Fronting)
            return;

        var limit = _timing.CurTime + ent.Comp.FrontDuration * 3;
        ent.Comp.FrontEnd = TimeSpan.FromTicks(Math.Min((ent.Comp.FrontEnd + time).Ticks, limit.Ticks));
    }

    public void ForceFront(Entity<DissociativeIdentityComponent> ent)
    {
        if (ent.Comp.Fronting ||
            ent.Comp.NextFront > _timing.CurTime ||
            !_mobState.IsAlive(ent) ||
            !TryComp<DissociativeAlterComponent>(ent.Comp.Alter, out var alterComp))
            return;

        var alter = new Entity<DissociativeAlterComponent>(ent.Comp.Alter.Value, alterComp);
        StartFront(ent, alter);
    }

    private bool IsSuppressed(DissociativeIdentityComponent comp)
        => _timing.CurTime < comp.LastAntipsychotic + comp.SuppressionWindow;

    private bool IsAbstinent(DissociativeIdentityComponent comp)
        => _timing.CurTime - comp.LastAntipsychotic >= comp.AbstinenceThreshold;

    private void OnFrontAction(Entity<DissociativeAlterComponent> ent, ref DissociativeFrontActionEvent args)
    {
        if (args.Handled || !TryGetHost(ent, out var maybeHost))
            return;

        args.Handled = true;
        var host = maybeHost.Value;

        if (host.Comp.Fronting)
        {
            _popup.PopupEntity(Loc.GetString("dissociative-not-in-control"), ent, ent);
            return;
        }

        if (!CanFront(host, ent, out var reason))
        {
            _popup.PopupEntity(reason, ent, ent);
            return;
        }

        StartFront(host, ent);
    }

    private bool CanFront(Entity<DissociativeIdentityComponent> host, Entity<DissociativeAlterComponent> alter, out string reason, bool voluntary = false)
    {
        reason = string.Empty;

        if (host.Comp.NextFront > _timing.CurTime)
        {
            var left = host.Comp.NextFront - _timing.CurTime;
            reason = Loc.GetString("dissociative-front-cooldown", ("minutes", (int) Math.Ceiling(left.TotalMinutes)));
            return false;
        }

        if (!voluntary && IsSuppressed(host.Comp))
        {
            reason = Loc.GetString("dissociative-front-suppressed");
            return false;
        }

        if (HasComp<MindShieldComponent>(host))
        {
            reason = Loc.GetString("dissociative-front-mindshield");
            return false;
        }

        if (!_mobState.IsAlive(host))
        {
            reason = Loc.GetString("dissociative-front-host-incapacitated");
            return false;
        }

        return _mind.TryGetMind(alter, out _, out _);
    }

    private void StartFront(Entity<DissociativeIdentityComponent> host, Entity<DissociativeAlterComponent> alter)
    {
        if (!_mind.TryGetMind(alter, out var alterMind, out _))
            return;

        EntityUid? hostMind = _mind.TryGetMind(host, out var hostMindId, out _) ? hostMindId : null;

        Retract(host, alter);

        host.Comp.HostMind = hostMind;
        host.Comp.AlterMind = alterMind;
        host.Comp.Fronting = true;

        SwapMinds(host, alter, hostMind, alterMind);

        var duration = host.Comp.FrontDuration;
        if (IsAbstinent(host.Comp))
            duration *= 2;

        host.Comp.FrontEnd = _timing.CurTime + duration;

        ApplyAlterLook(host, alter);

        _actions.AddAction(host, ref host.Comp.ReleaseActionEntity, host.Comp.ReleaseAction);

        _popup.PopupEntity(Loc.GetString("dissociative-front-start-alter"), host, host, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("dissociative-front-start-host"), alter, alter, PopupType.LargeCaution);

        _adminLog.Add(LogType.Mind, LogImpact.High,
            $"{ToPrettyString(alterMind):player} (alter) took over the body {ToPrettyString(host):target} from {ToPrettyString(hostMind):player} for {duration.TotalSeconds} seconds");
    }

    private void SwapMinds(EntityUid host, EntityUid alter, EntityUid? hostMind, EntityUid alterMind)
    {
        _swapping = true;

        if (hostMind != null)
            _mind.TransferTo(hostMind.Value, null, createGhost: false);

        _mind.TransferTo(alterMind, host);

        if (hostMind != null)
            _mind.TransferTo(hostMind.Value, alter);

        _swapping = false;
    }

    private void EndFront(Entity<DissociativeIdentityComponent> host)
    {
        if (!host.Comp.Fronting)
            return;

        host.Comp.Fronting = false;

        var alter = host.Comp.Alter;
        var hostMind = host.Comp.HostMind;
        var alterMind = host.Comp.AlterMind;

        _swapping = true;

        var alterInBody = TryComp<MindComponent>(alterMind, out var alterMindComp) && alterMindComp.OwnedEntity == host.Owner;
        if (alterInBody)
            _mind.TransferTo(alterMind!.Value, null, createGhost: false, mind: alterMindComp);

        if (TryComp<MindComponent>(hostMind, out var hostMindComp))
        {
            if (hostMindComp.VisitingEntity != null)
                _mind.UnVisit(hostMind.Value, hostMindComp);

            _mind.TransferTo(hostMind.Value, host, mind: hostMindComp);
        }

        if (alterInBody && alter != null && !TerminatingOrDeleted(alter.Value))
            _mind.TransferTo(alterMind!.Value, alter.Value, mind: alterMindComp);

        _swapping = false;

        RestoreHostLook(host);

        _actions.RemoveAction(host.Owner, host.Comp.ReleaseActionEntity);
        host.Comp.ReleaseActionEntity = null;

        var cooldown = host.Comp.FrontCooldown;
        if (IsAbstinent(host.Comp))
            cooldown /= 2;

        host.Comp.NextFront = _timing.CurTime + cooldown;
        host.Comp.HostMind = null;
        host.Comp.AlterMind = null;

        _popup.PopupEntity(Loc.GetString("dissociative-front-end-host"), host, host, PopupType.LargeCaution);
        if (alter != null)
            _popup.PopupEntity(Loc.GetString("dissociative-front-end-alter"), alter.Value, alter.Value, PopupType.Medium);

        _adminLog.Add(LogType.Mind, LogImpact.Medium,
            $"Alter takeover of {ToPrettyString(host):target} ended, control returned to {ToPrettyString(hostMind):player}");
    }

    private void ApplyAlterLook(Entity<DissociativeIdentityComponent> host, Entity<DissociativeAlterComponent> alter)
    {
        if (_visualBody.TryGatherMarkingsData(host.Owner, host.Comp.AlterLayers, out _, out _, out var hostApplied))
            host.Comp.StoredHostMarkings = CopyMarkings(hostApplied);

        if (_visualBody.TryGatherMarkingsData(alter.Owner, host.Comp.AlterLayers, out _, out _, out var alterApplied))
            _visualBody.ApplyMarkings(host, CopyMarkings(alterApplied));

        if (TryComp<TTSComponent>(host, out var hostTts) && TryComp<TTSComponent>(alter, out var alterTts))
        {
            host.Comp.StoredHostVoice = hostTts.VoicePrototypeId;
            hostTts.VoicePrototypeId = alterTts.VoicePrototypeId;
        }
    }

    private void RestoreHostLook(Entity<DissociativeIdentityComponent> host)
    {
        if (host.Comp.StoredHostMarkings is { } markings)
            _visualBody.ApplyMarkings(host, markings);

        if (host.Comp.StoredHostVoice is { } voice && TryComp<TTSComponent>(host, out var tts))
            tts.VoicePrototypeId = voice;

        host.Comp.StoredHostMarkings = null;
        host.Comp.StoredHostVoice = null;
    }

    private static Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> CopyMarkings(
        Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> source)
    {
        var result = new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();
        foreach (var (organ, layers) in source)
        {
            var copy = new Dictionary<HumanoidVisualLayers, List<Marking>>();
            foreach (var (layer, list) in layers)
                copy[layer] = new List<Marking>(list);

            result[organ] = copy;
        }

        return result;
    }

    private void OnGiveControlAction(Entity<DissociativeIdentityComponent> ent, ref DissociativeGiveControlActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Fronting)
        {
            _popup.PopupEntity(Loc.GetString("dissociative-not-in-control"), ent, ent);
            return;
        }

        if (ent.Comp.Alter is not { } alterUid || !TryComp<DissociativeAlterComponent>(alterUid, out var alter))
        {
            _popup.PopupEntity(Loc.GetString("dissociative-surrender-no-alter"), ent, ent);
            return;
        }

        if (!CanFront(ent, (alterUid, alter), out var reason, voluntary: true))
        {
            _popup.PopupEntity(string.IsNullOrEmpty(reason) ? Loc.GetString("dissociative-give-control-no-alter") : reason, ent, ent);
            return;
        }

        StartFront(ent, (alterUid, alter));
    }

    private void OnReleaseAction(Entity<DissociativeIdentityComponent> ent, ref DissociativeReleaseActionEvent args)
    {
        if (args.Handled || !ent.Comp.Fronting)
            return;

        args.Handled = true;
        ent.Comp.FrontEnd = _timing.CurTime;
    }

    private void OnHostStateChanged(Entity<DissociativeIdentityComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.Fronting && args.NewMobState != MobState.Alive)
            ent.Comp.FrontEnd = _timing.CurTime;
    }

    private void OnHostMindRemoved(Entity<DissociativeIdentityComponent> ent, ref MindRemovedMessage args)
    {
        if (ent.Comp.Fronting && !_swapping)
            ent.Comp.FrontEnd = _timing.CurTime;
    }

    private void OnHostExamined(Entity<DissociativeIdentityComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Fronting || !args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("dissociative-examine-fronting"));
    }
}
