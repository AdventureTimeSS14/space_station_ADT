using Content.Shared.ADT.Addiction.AddictedComponent;
using Content.Server.ADT.Addiction.EntityEffects;
using Content.Shared.Popups;
using Robust.Shared.Random;
using Robust.Shared.Timing;
namespace Content.Server.ADT.Addiction;

public sealed partial class AddictionSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<AddictedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Addicted)
            {
                comp.RemaningTime -= frameTime;
                if (comp.RemaningTime <= 0)
                {
                    comp.RemaningTime = comp.ChangeAddictionTypeTime;
                    if (comp.TypeAddiction > 0)
                    {
                        comp.TypeAddiction -= 1;
                    }

                }
            }
            else
            {
                if (comp.CurrentAddictedTime >= comp.RequiredTime)
                {
                    comp.Addicted = true;
                }
            }
        }
/*        var query = EntityQueryEnumerator<AddictedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime < comp.NextPopup)
                continue;
            if (comp.PopupMessages.Count <= 0)
                continue;
            var selfMessage = Loc.GetString(_random.Pick(comp.PopupMessages));
            _popup.PopupEntity(selfMessage, uid, uid);
            comp.NextPopup = _timing.CurTime + TimeSpan.FromSeconds(10);
        }*/
    }
}
