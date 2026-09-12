namespace Content.Client.ADT.Mirror;

[ByRefEvent]
public record struct CanBeSeenInMirrorsEvent()
{
    public bool Cancelled = false;
}
