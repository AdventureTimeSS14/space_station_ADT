using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Localizations;
using Content.Shared.ADT.StampHit;

namespace Content.Shared.ADT.StampHit;

public abstract partial class SharedStampHitSystem : EntitySystem
{
    public void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StampedEntityComponent, ExaminedEvent>(Examined);
    }
    private void Examined(EntityUid examinedUid, StampedEntityComponent stampedComp, ExaminedEvent args)
    {
        var locUser = ("user", Identity.Entity(examinedUid, EntityManager));
        List<string> readyListStamped = [];
        if (stampedComp.StampToEntity.Count!=0 && stampedComp.StampToEntity.Count!>10)
        {
            foreach (var stamp in stampedComp.StampToEntity)
            {
                readyListStamped.Add(Loc.GetString(stamp));
            }
           
            var locStamps = ("stamps", ContentLocalizationManager.FormatList(readyListStamped));
            args.PushMarkup(Loc.GetString("comp-stamp-examine", locUser, locStamps));
        }
    }

    public static void RemoveStamps(Entity<StampedEntityComponent> ent)
    {
        ent.Comp.StampToEntity.Clear();
    }
}
