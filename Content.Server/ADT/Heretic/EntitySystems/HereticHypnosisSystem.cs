using Content.Shared.ADT.Heretic.Components.Side;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Examine;
namespace Content.Server.ADT.Heretic.EntitySystems;
public sealed partial class HereticHypnosisSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HereticHypnosisComponent, ExaminedEvent>(OnExamine);
    }
    private void OnExamine(Entity<HereticHypnosisComponent> ent, ref ExaminedEvent args)
    {
        if (!_heretic.IsHereticOrGhoul(args.Examiner))
            return;
        if (string.IsNullOrWhiteSpace(ent.Comp.Order))
            return;
        args.PushMarkup(Loc.GetString("heretic-hypnosis-order", ("order", ent.Comp.Order)));
    }
}
