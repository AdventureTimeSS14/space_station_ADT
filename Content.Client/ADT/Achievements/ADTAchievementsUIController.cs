using Content.Client.ADT.Achievements.UI;
using Content.Shared.ADT.Achievements;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Client.ADT.Achievements;

[UsedImplicitly]
public sealed class ADTAchievementsUIController : UIController
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private static readonly SoundSpecifier UnlockSound =
        new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    private ADTAchievementsWindow? _window;

    private ADTAchievementSystem? System =>
        _entityManager.SystemOrNull<ADTAchievementSystem>();

    public void ToggleWindow()
    {
        if (_window is { IsOpen: true })
        {
            _window.Close();
            return;
        }

        OpenWindow();
    }

    public void OpenWindow()
    {
        var system = System;

        if (system == null)
            return;

        if (_window == null)
        {
            _window = UIManager.CreateWindow<ADTAchievementsWindow>();
            _window.OnClose += () => system.Updated -= OnUpdated;
        }

        system.Updated -= OnUpdated;
        system.Updated += OnUpdated;
        system.Unlocked -= OnUnlocked;
        system.Unlocked += OnUnlocked;

        system.RequestState();
        _window.UpdateState(system.States);
        _window.OpenCentered();
    }

    private void OnUpdated()
    {
        if (_window is { IsOpen: true } && System is { } system)
            _window.UpdateState(system.States);
    }

    private void OnUnlocked(ADTAchievementPrototype achievement)
    {
        _entityManager.System<SharedAudioSystem>().PlayGlobal(UnlockSound, Filter.Local(), false);
    }
}
