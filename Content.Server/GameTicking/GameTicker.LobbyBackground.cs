using Content.Server.GameTicking.Prototypes;
using Robust.Shared.Random;
using Content.Shared.ADT;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.GameTicking;

public sealed partial class GameTicker
{
    [ViewVariables]
    public string? LobbyBackground { get; private set; }

    [ViewVariables]
    private List<string>? _lobbyBackgrounds; // ADT Tweak

    private void InitializeLobbyBackground()
    {
        _lobbyBackgrounds = _prototypeManager.EnumeratePrototypes<AnimatedLobbyScreenPrototype>() // ADT Tweak
            .Select(x => x.Path)
            .ToList();

        RandomizeLobbyBackground();
    }

    private void RandomizeLobbyBackground()
    {
        LobbyBackground = _lobbyBackgrounds!.Any() ? _robustRandom.Pick(_lobbyBackgrounds!) : null; // ADT Tweak
    }
}
