// SPDX-FileCopyrightText: 2026 Kirill Golubenko
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT
//
// Author: Kirill Golubenko
// Discord: golub4ik
// Ckey: WikiHampter

using Content.Shared.Mind;
using Content.Shared.Mind.Filters;
using Content.Shared.Roles;
using Content.Shared.Whitelist;

namespace Content.Shared.ADT.BloodBrothers.Filters;

/// <summary>
/// Removes minds that have a role matching the blacklist.
/// </summary>
public sealed partial class FilterOutRoleMindFilter : MindFilter
{
    [DataField(required: true)]
    public EntityWhitelist Blacklist;

    protected override bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan, SharedMindSystem mindSys)
    {
        var roleSys = entMan.System<SharedRoleSystem>();
        return roleSys.MindHasRole(mind, Blacklist);
    }
}
