using System.Diagnostics.CodeAnalysis;
using Content.Shared.Preferences.Loadouts.Effects;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;

namespace Content.Shared._Ganimed.Preferences.Loadouts.Effects;

[DataDefinition]
public sealed partial class JobRestrictedLoadoutEffect : LoadoutEffect
{

    [DataField("whitelistJobs", required: true)]
    public List<string> WhitelistJobs { get; private set; } = new();

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        var allowed = false;
        foreach (var (jobId, priority) in profile.JobPriorities)
        {
            if (priority != JobPriority.High)
                continue;

            if (WhitelistJobs.Contains(jobId))
            {
                allowed = true;
                break;
            }
        }

        if (!allowed)
        {
            var protoManager = collection.Resolve<IPrototypeManager>();

            var localizedJobs = new List<string>();
            foreach (var jobId in WhitelistJobs)
            {
                if (protoManager.TryIndex<JobPrototype>(jobId, out var jobProto))
                    localizedJobs.Add(Loc.GetString(jobProto.LocalizedName));
                else
                    localizedJobs.Add(jobId); // fallback
            }

            reason = FormattedMessage.FromMarkupOrThrow(
                Loc.GetString("loadout-jobrestricted-fail", ("jobs", string.Join(", ", localizedJobs)))
            );
            return false;
        }

        return true;
    }
}
