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
+        List<string> readyListStamped = [];
+        foreach (var stamp in stampedComp.StampToEntity)
+        {
+            readyListStamped.Add(Loc.GetString(stamp));
         }
+        var locStamps = ("stamps", ContentLocalizationManager.FormatList(readyListStamped));
+        args.PushMarkup(Loc.GetString("comp-stamp-examine", locUser, locStamps));
        }
    }
}
