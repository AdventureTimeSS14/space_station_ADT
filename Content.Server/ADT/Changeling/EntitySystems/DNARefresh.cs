using Content.Shared.Alert;
using Content.Server.Abilities.Mime;
using Content.Shared.Changeling.Components;

namespace Content.Server.Changeling.EntitySystems;

[DataDefinition]
public sealed partial class DNARefresh : IAlertClick
{
    public void AlertClicked(EntityUid player)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();
        if (entManager.TryGetComponent(player, out ChangelingComponent? component))
        {
            entManager.System<ChangelingSystem>().Refresh(player, component);
        }
    }
}
