using Content.Shared.ADT.IpcScreen;
using Robust.Shared.Player;

namespace Content.Server.ADT.IpcScreen;

public sealed partial class IpcScreenSystem
{
    private void InitializeIpcScreenAbilities()
    {
        SubscribeLocalEvent<IpcScreenComponent, IpcScreenActionEvent>(IpcScreenAction);
    }

    private void IpcScreenAction(EntityUid uid, IpcScreenComponent comp, IpcScreenActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _uiSystem.TryOpenUi(uid, IpcScreenUiKey.Key, actor.Owner);

        UpdateInterface(uid, comp);

        args.Handled = true;
    }
}
