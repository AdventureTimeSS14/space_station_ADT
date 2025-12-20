using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Shared.ADT.Posing;

public abstract partial class SharedPosingSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.TogglePosing,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            TogglePosing(userUid);
                    },
                    handle: false));
    }

    private void TogglePosing(EntityUid uid, PosingComponent? posingComp = null)
    {
        if (!Resolve(uid, ref posingComp))
            return;

        posingComp.Posing = !posingComp.Posing;
        ClientTogglePosing(uid, posingComp.Posing);
        Dirty(uid, posingComp);
    }

    protected virtual void ClientTogglePosing(EntityUid uid, bool posing)
    {
    }
}
