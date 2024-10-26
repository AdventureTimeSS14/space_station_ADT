using Content.Client.Gameplay;
using Content.Client.Ghost;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Shared.Ghost;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Content.Client.UserInterface.Systems.Ghost;

namespace Content.Client.UserInterface.Systems.Ghost;

// TODO hud refactor BEFORE MERGE fix ghost gui being too far up
public sealed partial class GhostUIController
{
    private void GhostBarPressed()
    {
        Gui?.GhostBarWindow.OpenCentered();
    }

    private void GhostBarSpawnPressed()
    {
        _system?.GhostBarSpawn();
    }

    public void LoadGhostbarGui()
    {
        if (Gui == null)
            return;

        Gui.GhostBarPressed += GhostBarPressed;
        Gui.GhostBarWindow.SpawnButtonPressed += GhostBarSpawnPressed;
    }

    public void UnloadGhostbarGui()
    {
        if (Gui == null)
            return;

        Gui.GhostBarPressed -= GhostBarPressed;
        Gui.GhostBarWindow.SpawnButtonPressed -= GhostBarSpawnPressed;
    }
}
