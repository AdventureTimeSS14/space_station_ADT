using Content.Server.ADT.Dissociative.Components;
using Content.Shared.ADT.Dissociative;
using Content.Shared.ADT.TTS;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Popups;

namespace Content.Server.ADT.Dissociative;

public sealed partial class DissociativeIdentitySystem
{
    private void InitializeSurrender()
    {
        SubscribeLocalEvent<DissociativeIdentityComponent, DissociativeSurrenderActionEvent>(OnSurrenderAction);
    }

    private void OnSurrenderAction(Entity<DissociativeIdentityComponent> ent, ref DissociativeSurrenderActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Fronting)
        {
            _popup.PopupEntity(Loc.GetString("dissociative-not-in-control"), ent, ent);
            return;
        }

        if (ent.Comp.Alter is not { } alterUid ||
            !TryComp<DissociativeAlterComponent>(alterUid, out var alter) ||
            !_mind.TryGetMind(alterUid, out var alterMind, out _))
        {
            _popup.PopupEntity(Loc.GetString("dissociative-surrender-no-alter"), ent, ent);
            return;
        }

        if (!_mind.TryGetMind(ent, out var hostMind, out var hostMindComp))
            return;

        Surrender(ent, (alterUid, alter), (hostMind, hostMindComp), alterMind);
    }

    private void Surrender(Entity<DissociativeIdentityComponent> host,
        Entity<DissociativeAlterComponent> alter,
        Entity<MindComponent> hostMind,
        EntityUid alterMind)
    {
        Retract(host, alter);

        if (!alter.Comp.SetupDone)
            FinishSetup(alter);

        SwapLooks(host, alter);

        _meta.SetEntityName(alter, hostMind.Comp.CharacterName ?? Name(host));

        SwapMinds(host, alter, hostMind, alterMind);

        host.Comp.NextFront = _timing.CurTime + host.Comp.FrontCooldown;

        _popup.PopupEntity(Loc.GetString("dissociative-surrender-alter"), host, host, PopupType.LargeCaution);
        _popup.PopupEntity(Loc.GetString("dissociative-surrender-host"), alter, alter, PopupType.LargeCaution);

        _adminLog.Add(LogType.Mind, LogImpact.High,
            $"{ToPrettyString(hostMind):player} gave up the body {ToPrettyString(host):target} for good to its alter {ToPrettyString(alterMind):player}");
    }

    private void SwapLooks(Entity<DissociativeIdentityComponent> host, Entity<DissociativeAlterComponent> alter)
    {
        if (_visualBody.TryGatherMarkingsData(host.Owner, host.Comp.AlterLayers, out _, out _, out var hostApplied) &&
            _visualBody.TryGatherMarkingsData(alter.Owner, host.Comp.AlterLayers, out _, out _, out var alterApplied))
        {
            var hostCopy = CopyMarkings(hostApplied);
            _visualBody.ApplyMarkings(host, CopyMarkings(alterApplied));
            _visualBody.ApplyMarkings(alter, hostCopy);
        }

        if (TryComp<TTSComponent>(host, out var hostTts) && TryComp<TTSComponent>(alter, out var alterTts))
            (hostTts.VoicePrototypeId, alterTts.VoicePrototypeId) = (alterTts.VoicePrototypeId, hostTts.VoicePrototypeId);

        if (TryComp<HumanoidProfileComponent>(host, out var hostProfile) &&
            TryComp<HumanoidProfileComponent>(alter, out var alterProfile))
            ApplySex((alter, alterProfile), hostProfile.Sex, hostProfile.Gender);
    }
}
