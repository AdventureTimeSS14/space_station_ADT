using Content.Shared.Examine;
using Content.Shared.IdentityManagement;

namespace Content._SD.Server.DeathSquad;

public sealed partial class DeathSquadMemberSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DeathSquadMemberComponent, ExaminedEvent>(OnExamined);
    }


    private void OnExamined(Entity<DeathSquadMemberComponent> deathSquad, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var details = Loc.GetString("death-squad-examined", ("target", Identity.Entity(deathSquad, EntityManager)));
        args.PushMarkup(details, 5);
    }
}
