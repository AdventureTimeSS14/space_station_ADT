using Content.Server.GameTicking;
using Content.Shared.ADT.Paper;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Content.Shared.Station;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Paper;

public sealed class PaperFieldSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public PaperFieldContext? GetFieldContext(EntityUid user)
    {
        if (!_mind.TryGetMind(user, out var mindId, out var mindComp))
            return null;

        var context = new PaperFieldContext();

        context.CharacterName = mindComp.CharacterName ?? Loc.GetString("paper-field-unknown");

        context.Job = _job.MindTryGetJobName(mindId);

        var roundTime = _gameTicker.RoundDuration();
        context.CurrentTime = $"{roundTime.Hours:D2}:{roundTime.Minutes:D2}";
        context.CurrentDate = DateTime.Now.ToString("yyyy-MM-dd");

        var stationEnt = _station.GetOwningStation(user);
        context.StationName = stationEnt != null ? Name(stationEnt.Value) : Loc.GetString("paper-field-unknown");

        if (TryComp<HumanoidProfileComponent>(user, out var profile))
        {
            context.Gender = profile.Sex switch
            {
                Sex.Male => Loc.GetString("paper-field-sex-male"),
                Sex.Female => Loc.GetString("paper-field-sex-female"),
                _ => Loc.GetString("paper-field-sex-unsexed")
            };

            if (_proto.TryIndex<SpeciesPrototype>(profile.Species, out var speciesProto))
                context.Race = Loc.GetString(speciesProto.Name);
            else
                context.Race = profile.Species;
        }

        return context;
    }
}
