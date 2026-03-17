using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Localizations;
using Content.Shared.ADT.StampHit;

namespace Content.Shared.ADT.StampHit;

public abstract partial class SharedStampHitSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StampedEntityComponent, ExaminedEvent>(Examined);
    }
    private void Examined(EntityUid examinedUid, StampedEntityComponent stampedComp, ExaminedEvent args)
    {
        var locUser = ("user", Identity.Entity(examinedUid, EntityManager));
        List<string> readyListStamped = [];
        if (stampedComp.StampToEntity.Count==0)
            return;
        for (var i = 0; i < stampedComp.StampToEntity.Count && i < 10; i++)
        {
            readyListStamped.Add(Loc.GetString(stampedComp.StampToEntity[i]));
        }
           
        var locStamps = ("stamps", ContentLocalizationManager.FormatList(readyListStamped));
        args.PushMarkup(Loc.GetString("comp-stamp-examine", locUser, locStamps));
    }

    public static void RemoveStamps(Entity<StampedEntityComponent> ent)
    {
        ent.Comp.StampToEntity.Clear();
    }
}
