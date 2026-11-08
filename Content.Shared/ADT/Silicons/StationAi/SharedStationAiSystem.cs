using Content.Shared.StationAi;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    public void SetVisionNetwork(Entity<StationAiVisionComponent> ent, string? visionNetwork)
    {
        ent.Comp.VisionNetwork = visionNetwork;
        Dirty(ent.Owner, ent.Comp);
    }
}