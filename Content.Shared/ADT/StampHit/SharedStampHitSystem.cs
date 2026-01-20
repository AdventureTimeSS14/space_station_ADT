using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Localizations;
using Content.Shared.ADT.StampHit;

namespace Content.Shared.ADT.StampHit;

public abstract partial class SharedStampHitSystem : EntitySystem
{
    private void InitializeInteractions()
    {
        SubscribeLocalEvent<StampedEntityComponent, ExaminedEvent>(Examined);
    }
    private void Examined(EntityUid examinedUid, StampedEntityComponent stampedComp, ExaminedEvent args)
    {
        var locUser = ("user", Identity.Entity(examinedUid, EntityManager));
        (string, object) locStamps;
        if (TryComp<StampedEntityComponent>(examinedUid, out var stampedEntity))
        {
            List<string> readyListStamped = [];
            foreach (var i in stampedEntity.StampToEntity)
            {
                readyListStamped.Add(Loc.GetString(i));
            }
            locStamps = ("stamps", ContentLocalizationManager.FormatList(readyListStamped));
            args.PushMarkup(Loc.GetString("comp-stamp-examine", locUser, locStamps));
        }
    }
}