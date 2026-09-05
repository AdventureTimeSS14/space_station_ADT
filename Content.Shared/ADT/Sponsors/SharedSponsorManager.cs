using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Sponsors;

public abstract class SharedSponsorManager : ISharedSponsorManager
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;

    private FrozenDictionary<string, string[]>? _jobDepartments;

    public virtual void Initialize()
    {
        PrototypeManager.PrototypesReloaded += OnPrototypesReloaded;
    }

    public virtual void Shutdown()
    {
        PrototypeManager.PrototypesReloaded -= OnPrototypesReloaded;
    }

    public abstract SponsorData GetData(ICommonSession? session);

    public bool TryGetData(ICommonSession? session, [NotNullWhen(true)] out SponsorData? data)
    {
        var result = GetData(session);

        if (!result.HasAnyBenefit)
        {
            data = null;
            return false;
        }

        data = result;
        return true;
    }

    public virtual bool IsLoadoutAllowed(ICommonSession? session, string loadoutId)
    {
        return GetData(session).IsLoadoutAllowed(loadoutId);
    }

    public virtual bool IsMarkingAllowed(ICommonSession? session, string markingId)
    {
        return GetData(session).IsMarkingAllowed(markingId);
    }

    public virtual bool IsSpeciesAllowed(ICommonSession? session, string speciesId)
    {
        return GetData(session).IsSpeciesAllowed(speciesId);
    }

    public virtual bool IsTraitAllowed(ICommonSession? session, string traitId)
    {
        return GetData(session).IsTraitAllowed(traitId);
    }

    public virtual bool IsTtsVoiceAllowed(ICommonSession? session, string voiceId)
    {
        return GetData(session).IsTtsVoiceAllowed(voiceId);
    }

    public bool IsJobTimeBypassed(ICommonSession? session, ProtoId<JobPrototype> job)
    {
        var data = GetData(session);

        if ((data.RoleBypass & SponsorRoleBypass.Jobs) == 0)
            return false;

        if (data.ExcludedJobs.Contains(job.Id))
            return false;

        if (data.ExcludedDepartments.Count > 0 && GetJobDepartments().TryGetValue(job.Id, out var departments))
        {
            foreach (var department in departments)
            {
                if (data.ExcludedDepartments.Contains(department))
                    return false;
            }
        }

        return true;
    }

    public bool IsAntagTimeBypassed(ICommonSession? session)
    {
        return GetData(session).IsAntagTimeBypassed();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<DepartmentPrototype>())
            return;

        _jobDepartments = null;
    }

    private FrozenDictionary<string, string[]> GetJobDepartments()
    {
        return _jobDepartments ??= BuildDepartmentMap();
    }

    private FrozenDictionary<string, string[]> BuildDepartmentMap()
    {
        var map = new Dictionary<string, List<string>>();

        foreach (var department in PrototypeManager.EnumeratePrototypes<DepartmentPrototype>())
        {
            foreach (var job in department.Roles)
            {
                if (!map.TryGetValue(job.Id, out var departments))
                {
                    departments = new List<string>();
                    map[job.Id] = departments;
                }

                departments.Add(department.ID);
            }
        }

        var frozen = new Dictionary<string, string[]>(map.Count);

        foreach (var (job, departments) in map)
        {
            frozen[job] = departments.ToArray();
        }

        return frozen.ToFrozenDictionary();
    }
}
