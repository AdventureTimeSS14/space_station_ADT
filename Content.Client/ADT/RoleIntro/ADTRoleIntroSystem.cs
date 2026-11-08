using Content.Shared.ADT.RoleIntro;

namespace Content.Client.ADT.RoleIntro;

public sealed class ADTRoleIntroSystem : EntitySystem
{
    private ADTRoleIntroWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ADTRoleIntroEvent>(OnRoleIntro);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _window?.Orphan();
        _window = null;
    }

    private void OnRoleIntro(ADTRoleIntroEvent ev)
    {
        _window?.Orphan();

        _window = new ADTRoleIntroWindow(Loc.GetString(ev.Title), Loc.GetString(ev.Text), ev.LockTime);
        _window.OnClose += () => _window = null;
        _window.OpenCentered();
    }
}
