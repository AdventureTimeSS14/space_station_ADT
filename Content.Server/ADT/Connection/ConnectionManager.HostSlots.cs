using System.Linq;
using Content.Server.Database;
using Content.Shared.ADT.CCVar;
using Content.Shared.Administration;
using Robust.Server;

namespace Content.Server.Connection;

public sealed partial class ConnectionManager
{
    [Dependency] private IBaseServer _baseServer = default!;

    private bool IsHostReservedSlotsDenied(Admin? adminData)
    {
        var reserved = _cfg.GetCVar(ADTCCVars.HostReservedSlots);
        if (reserved <= 0 || _plyMgr.PlayerCount < _baseServer.MaxPlayers - reserved)
            return false;

        return !HasHostFlag(adminData);
    }

    private static bool HasHostFlag(Admin? adminData)
    {
        if (adminData == null || adminData.Suspended)
            return false;

        var flags = AdminFlags.None;

        if (adminData.AdminRank != null)
            flags = AdminFlagsHelper.NamesToFlags(adminData.AdminRank.Flags.Select(p => p.Flag));

        foreach (var dbFlag in adminData.Flags)
        {
            var flag = AdminFlagsHelper.NameToFlag(dbFlag.Flag);
            if (dbFlag.Negative)
                flags &= ~flag;
            else
                flags |= flag;
        }

        return (flags & AdminFlags.Host) != 0;
    }
}
