using System.Linq;
using Content.Client.Lobby;
using Content.Shared.ADT.Roadmap;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Roadmap;

public sealed class RoadmapUIController : UIController
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    private RoadmapWindow? _window;

    public void ToggleRoadmap()
    {
        if (_window != null)
        {
            _window.Close();
            return;
        }

        _window = new RoadmapWindow();
        _window.OnClose += () => _window = null;

        _window.OpenCentered();
        _window.Populate(_proto.EnumeratePrototypes<RoadmapItemPrototype>().ToList());
    }
}
