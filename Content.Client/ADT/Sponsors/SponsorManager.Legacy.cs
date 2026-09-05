using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client.Corvax.Sponsors;
using Content.Shared.ADT.Sponsors;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using LegacySponsorInfo = Content.Shared.Corvax.Sponsors.SponsorInfo;

namespace Content.Client.ADT.Sponsors;

public sealed partial class SponsorManager
{
    [Dependency] private readonly SponsorsManager _legacy = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _legacyBridge;

    private void InitializeLegacy()
    {
        _cfg.OnValueChanged(SponsorCVars.LegacyBridge, OnLegacyBridgeChanged, true);
    }

    private void ShutdownLegacy()
    {
        _cfg.UnsubValueChanged(SponsorCVars.LegacyBridge, OnLegacyBridgeChanged);
    }

    public override bool IsLoadoutAllowed(ICommonSession? session, string loadoutId)
    {
        if (base.IsLoadoutAllowed(session, loadoutId))
            return true;

        return IsLegacySponsor(session);
    }

    public override bool IsMarkingAllowed(ICommonSession? session, string markingId)
    {
        if (base.IsMarkingAllowed(session, markingId))
            return true;

        if (!TryGetLegacyInfo(session, out var info))
            return false;

        return info.AllowedMarkings.Contains(markingId);
    }

    public override bool IsSpeciesAllowed(ICommonSession? session, string speciesId)
    {
        if (base.IsSpeciesAllowed(session, speciesId))
            return true;

        return IsLegacySponsor(session);
    }

    public override bool IsTraitAllowed(ICommonSession? session, string traitId)
    {
        if (base.IsTraitAllowed(session, traitId))
            return true;

        return IsLegacySponsor(session);
    }

    private bool IsLegacySponsor(ICommonSession? session)
    {
        if (!TryGetLegacyInfo(session, out var info))
            return false;

        return info.Tier > 0;
    }

    private bool TryGetLegacyInfo(ICommonSession? session, [NotNullWhen(true)] out LegacySponsorInfo? info)
    {
        info = null;

        if (!_legacyBridge || session == null)
            return false;

        if (_players.LocalSession == null || session.UserId != _players.LocalSession.UserId)
            return false;

        if (!_legacy.TryGetInfo(out var legacyInfo))
            return false;

        if (legacyInfo.ExpireDate.ToLocalTime() <= DateTime.Now)
            return false;

        info = legacyInfo;
        return true;
    }

    private void OnLegacyBridgeChanged(bool value)
    {
        if (_legacyBridge == value)
            return;

        _legacyBridge = value;

        RaiseUpdated();
    }
}
