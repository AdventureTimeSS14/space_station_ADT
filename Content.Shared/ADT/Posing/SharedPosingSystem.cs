using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Input;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Robust.Shared.Input.Binding;

namespace Content.Shared.ADT.Posing;

public abstract partial class SharedPosingSystem : EntitySystem
{
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PosingComponent, UpdateCanMoveEvent>(OnUpdateCanMove);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.TogglePosing,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            TogglePosing(userUid);
                    },
                    handle: false))
            .Bind(ContentKeyFunctions.PosingOffsetRight,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            TryAdjustPosingOffset(userUid, new(0.05f, 0f));
                    },
                    handle: false))
            .Bind(ContentKeyFunctions.PosingOffsetLeft,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            TryAdjustPosingOffset(userUid, new(-0.05f, 0f));
                    },
                    handle: false))
            .Bind(ContentKeyFunctions.PosingOffsetUp,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            TryAdjustPosingOffset(userUid, new(0f, 0.05f));
                    },
                    handle: false))
            .Bind(ContentKeyFunctions.PosingOffsetDown,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            TryAdjustPosingOffset(userUid, new(0f, -0.05f));
                    },
                    handle: false))
            .Bind(ContentKeyFunctions.PosingRotatePositive,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            TryAdjustPosingAngle(userUid, -5f);
                    },
                    handle: false))
            .Bind(ContentKeyFunctions.PosingRotateNegative,
                InputCmdHandler.FromDelegate(session =>
                    {
                        if (session?.AttachedEntity is { } userUid)
                            TryAdjustPosingAngle(userUid, 5f);
                    },
                    handle: false))
            .Register<SharedPosingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedPosingSystem>();
    }

    private void OnUpdateCanMove(EntityUid uid, PosingComponent component, UpdateCanMoveEvent args)
    {
        if (component.Posing)
            args.Cancel();
    }

    private void TogglePosing(EntityUid uid, PosingComponent? posingComp = null)
    {
        if (!Resolve(uid, ref posingComp, false))
            return;

        if (_standing.IsDown(uid))
            return;

        posingComp.Posing = !posingComp.Posing;
        _actionBlocker.UpdateCanMove(uid);

        posingComp.CurrentAngle = Angle.Zero;
        posingComp.CurrentOffset = Vector2.Zero;

        ClientTogglePosing(uid, posingComp);
        Dirty(uid, posingComp);
    }

    private void TryAdjustPosingOffset(EntityUid uid, Vector2 offset, PosingComponent? posingComp = null)
    {
        if (!Resolve(uid, ref posingComp, false) || !posingComp.Posing)
            return;

        var previusOffset = posingComp.CurrentOffset;

        posingComp.CurrentOffset += offset;
        posingComp.CurrentOffset = Vector2.Clamp(posingComp.CurrentOffset, -posingComp.OffsetLimits, posingComp.OffsetLimits);

        if (posingComp.CurrentOffset.Equals(previusOffset))
            return;

        Dirty(uid, posingComp);
    }

    private void TryAdjustPosingAngle(EntityUid uid, float angle, PosingComponent? posingComp = null)
    {
        if (!Resolve(uid, ref posingComp, false) || !posingComp.Posing)
            return;

        var previusAngle = posingComp.CurrentAngle;

        var newAngle = posingComp.CurrentAngle.Degrees + angle;
        posingComp.CurrentAngle = Angle.FromDegrees(Math.Clamp(newAngle, -posingComp.AngleLimits, posingComp.AngleLimits));

        if (posingComp.CurrentAngle.Equals(previusAngle))
            return;

        Dirty(uid, posingComp);
    }

    protected virtual void ClientTogglePosing(EntityUid uid, PosingComponent posing)
    {
    }
}
