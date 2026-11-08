using Content.Shared.ADT.Polls;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Polls.UI;

public static class PollNames
{
    public static string Job(IPrototypeManager proto, string id)
    {
        return proto.TryIndex<JobPrototype>(id, out var job) ? job.LocalizedName : id;
    }

    public static string Department(IPrototypeManager proto, string id)
    {
        return proto.TryIndex<DepartmentPrototype>(id, out var department) ? Loc.GetString(department.Name) : id;
    }

    public static string Species(IPrototypeManager proto, string id)
    {
        return proto.TryIndex<SpeciesPrototype>(id, out var species) ? Loc.GetString(species.Name) : id;
    }

    public static string Antag(IPrototypeManager proto, string id)
    {
        return proto.TryIndex<AntagPrototype>(id, out var antag) ? Loc.GetString(antag.Name) : id;
    }

    public static string Group(IPrototypeManager proto, PollGroupBy groupBy, string id)
    {
        if (id.Length == 0)
            return Loc.GetString("adt-poll-stats-group-none");

        return groupBy switch
        {
            PollGroupBy.Department => Department(proto, id),
            PollGroupBy.Job => Job(proto, id),
            PollGroupBy.Species => Species(proto, id),
            _ => id,
        };
    }

    public static string ShowMode(PollShowMode mode)
    {
        return mode switch
        {
            PollShowMode.Immediately => Loc.GetString("adt-poll-mode-immediately"),
            PollShowMode.RoundStart => Loc.GetString("adt-poll-mode-round-start"),
            _ => Loc.GetString("adt-poll-mode-round-end"),
        };
    }

    public static string QuestionType(PollQuestionType type)
    {
        return type switch
        {
            PollQuestionType.Single => Loc.GetString("adt-poll-type-single"),
            PollQuestionType.Multiple => Loc.GetString("adt-poll-type-multiple"),
            PollQuestionType.Rating => Loc.GetString("adt-poll-type-rating"),
            _ => Loc.GetString("adt-poll-type-text"),
        };
    }
}
