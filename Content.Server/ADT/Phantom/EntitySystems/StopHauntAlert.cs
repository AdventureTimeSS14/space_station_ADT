using Content.Server.ADT.Phantom.EntitySystems;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.Alert;
using JetBrains.Annotations;

namespace Content.Server.ADT.Phantom;

/// <summary>
/// Resist fire
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class StopHaunt : IAlertClick
{
    public void AlertClicked(EntityUid player)
    {
        var entManager = IoCManager.Resolve<IEntityManager>();

        if (entManager.TryGetComponent(player, out PhantomComponent? phantom))
        {
            entManager.System<PhantomSystem>().StopHaunt(player, phantom.Holder);
        }
    }
}
