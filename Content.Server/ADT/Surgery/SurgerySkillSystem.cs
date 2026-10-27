using Content.Shared.ADT.Surgery.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;

namespace Content.Server.ADT.Surgery;

public sealed class SurgerySkillSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _role = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleAddedEvent>(OnRoleAdded);
        SubscribeLocalEvent<MindContainerComponent, MindAddedMessage>(OnMindAdded);
    }

    private void OnRoleAdded(RoleAddedEvent args)
    {
        if (args.Mind.OwnedEntity is { } body && _role.MindIsAntagonist(args.MindId))
            EnsureComp<SurgerySkillComponent>(body);
    }

    private void OnMindAdded(Entity<MindContainerComponent> ent, ref MindAddedMessage args)
    {
        if (_role.MindIsAntagonist(args.Mind))
            EnsureComp<SurgerySkillComponent>(ent);
    }
}
