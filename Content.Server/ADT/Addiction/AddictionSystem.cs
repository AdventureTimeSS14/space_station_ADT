using Content.Shared.ADT.Addiction.AddictedComponent;
using Content.Server.ADT.Addiction.EntityEffects;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Timing;
namespace Content.Server.ADT.Addiction;

public sealed partial class AddictionSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        TimeSpan fT = TimeSpan.FromSeconds(frameTime);
        var query = EntityQueryEnumerator<AddictedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateCurrentAddictedTime(comp, fT);
            UpdateTypeAddiction(comp);
            UpdateAddicted(comp);
        }
    }
    public void UpdateCurrentAddictedTime(AddictedComponent comp, TimeSpan frameTime)
    {
        if (comp.CurrentAddictedTime >= TimeSpan.Zero + frameTime)
            comp.CurrentAddictedTime -= frameTime;
        else
            comp.CurrentAddictedTime = TimeSpan.Zero;
        if (comp.RemaningTime >= TimeSpan.Zero + frameTime)
            comp.RemaningTime -= frameTime;
        else
            comp.RemaningTime = TimeSpan.Zero;
    }
    public void UpdateTypeAddiction(AddictedComponent comp)
    {
        if (comp.TypeAddiction > 0 && comp.Addicted && comp.RemaningTime <= TimeSpan.Zero)
        {
            comp.RemaningTime = comp.ChangeAddictionTypeTime;
            comp.TypeAddiction -= 1;
        }
    }
    public void UpdateAddicted(AddictedComponent comp)
    {
        if (comp.CurrentAddictedTime >= comp.RequiredTime)
            comp.Addicted = true;
    }
}
