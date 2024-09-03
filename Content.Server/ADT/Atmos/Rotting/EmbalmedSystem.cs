using Content.Shared.ADT.Atmos.Miasma;
using Content.Shared.Examine;
using Content.Shared.Mobs.Systems;

namespace Content.Server.ADT.Atmos.Rotting;

public sealed partial class EmbalmedSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<EmbalmedComponent, ExaminedEvent>(OnExamine);
        base.Initialize();
    }

    private void OnExamine(EntityUid uid, EmbalmedComponent component, ExaminedEvent args)
    {
        if (!_mobState.IsDead(uid))
            return;
        args.PushMarkup(Loc.GetString("adt-rotting-embalmed"));
    }
}