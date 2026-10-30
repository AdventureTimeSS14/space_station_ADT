using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.ReagentEffects;
using Content.Shared.Body;
using Content.Shared.EntityEffects;

namespace Content.Server.ADT.Surgery;

public sealed partial class CureLobotomyEffectSystem : EntityEffectSystem<BodyComponent, CureLobotomy>
{
    [Dependency] private BodySystem _body = default!;

    protected override void Effect(Entity<BodyComponent> entity, ref EntityEffectEvent<CureLobotomy> args)
    {
        foreach (var organ in _body.EnumerateOrgans<LobotomizedOrganComponent>(entity.Owner))
        {
            RemComp<LobotomizedOrganComponent>(organ.Owner);
        }
    }
}
