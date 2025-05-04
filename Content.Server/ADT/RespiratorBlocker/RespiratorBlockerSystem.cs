using Content.Server.Body.Systems;

namespace Content.Server.ADT.RespiratorBlocker;

public sealed partial class RespiratorBlockSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BreathBlockComponent, InhaleLocationEvent>(OnBreath);
    }

    private void OnBreath(EntityUid uid, BreathBlockComponent comp, InhaleLocationEvent args)
    {
        ///ПОЧЕМУ ЭТО НЕ РАБОТАЕТ
        args.Gas = null;
    }
}
