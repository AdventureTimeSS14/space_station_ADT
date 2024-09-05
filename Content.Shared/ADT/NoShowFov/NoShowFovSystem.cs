using Content.Shared.Inventory.Events;
using Content.Shared.ADT.NoShowFov;

namespace Content.Client.ADT.NoShowFov;

public sealed class NoShowFovSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NoShowFovComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<NoShowFovComponent, GotUnequippedEvent>(OnUnequipped);
    }

    private void OnEquipped(EntityUid uid, NoShowFovComponent component, ref GotEquippedEvent args)
    {
        ToggleFov(args.Equipee, false);
    }

    private void OnUnequipped(EntityUid uid, NoShowFovComponent component, ref GotUnequippedEvent args)
    {
        ToggleFov(args.Equipee, true);
    }

    private void ToggleFov(EntityUid entity, bool drawFov)
    {
        if (TryComp<EyeComponent>(entity, out var _))
        {
            _eye.SetDrawFov(entity, drawFov);
        }
    }
}


