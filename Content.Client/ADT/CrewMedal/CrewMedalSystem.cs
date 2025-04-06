using Content.Client.ADT.CrewMedal.UI;
using Content.Shared.ADT.CrewMedal;

namespace Content.Client.ADT.CrewMedal;

/// <summary>
/// Handles the client-side logic for the Crew Medal system.
/// </summary>
public sealed class CrewMedalSystem : SharedCrewMedalSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        // Subscribes to the event triggered after the state is automatically handled.
        SubscribeLocalEvent<CrewMedalComponent, AfterAutoHandleStateEvent>(OnCrewMedalAfterState);
    }

    /// <summary>
    /// When an updated state is received on the client, refresh the UI to display the latest data.
    /// </summary>
    private void OnCrewMedalAfterState(Entity<CrewMedalComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        // Checks if the Crew Medal UI is open for the given entity and reloads it with updated data.
        if (_userInterfaceSystem.TryGetOpenUi<CrewMedalBoundUserInterface>(
                entity.Owner,
                CrewMedalUiKey.Key,
                out var medalUi))
        {
            medalUi.Reload();
        }
    }
}
