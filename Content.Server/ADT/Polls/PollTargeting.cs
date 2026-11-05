using Content.Shared.ADT.Polls;

namespace Content.Server.ADT.Polls;

public static class PollTargeting
{
    public static bool Matches(PollFilters filters, PollSnapshot snapshot)
    {
        if (filters.NeedsRound() && !snapshot.InRound)
            return false;

        if (filters.Jobs.Count > 0 && !filters.Jobs.Overlaps(snapshot.Jobs))
            return false;

        if (filters.Departments.Count > 0 && !filters.Departments.Overlaps(snapshot.Departments))
            return false;

        if (filters.Species.Count > 0 && !filters.Species.Overlaps(snapshot.Species))
            return false;

        if (filters.Antag == PollAntagFilter.Only && !snapshot.Antag)
            return false;

        if (filters.Antag == PollAntagFilter.Exclude && snapshot.Antag)
            return false;

        if (filters.AntagRoles.Count > 0 && !filters.AntagRoles.Overlaps(snapshot.AntagRoles))
            return false;

        if (filters.MinPlaytimeHours is { } minHours && snapshot.PlaytimeMinutes < minHours * 60)
            return false;

        if (filters.MaxPlaytimeHours is { } maxHours && snapshot.PlaytimeMinutes > maxHours * 60)
            return false;

        if (string.IsNullOrEmpty(filters.Tracker))
            return true;

        var trackerMinutes = snapshot.TrackerMinutes.GetValueOrDefault(filters.Tracker);

        if (filters.TrackerMinHours is { } trackerMin && trackerMinutes < trackerMin * 60)
            return false;

        if (filters.TrackerMaxHours is { } trackerMax && trackerMinutes > trackerMax * 60)
            return false;

        return true;
    }
}
