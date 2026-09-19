using Content.Server.Temperature.Components;
using Content.Server.Light.Components;
using Content.Shared.ADT.Lavaland;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Weather.Components;
using Content.Shared.ADT.Weather.Components;
using Content.Shared.Light.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTTorchHolderSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTTorchHolderComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTTorchHolderComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ADTTorchHolderComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<ADTTorchHolderComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<ADTTorchHolderComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<ADTTorchHolderComponent> ent, ref MapInitEvent args)
    {
        UpdateStatus(ent);
    }

    private void OnInserted(Entity<ADTTorchHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        UpdateStatus(ent);
    }

    private void OnRemoved(Entity<ADTTorchHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        UpdateStatus(ent);
    }

    private void OnInteractHand(Entity<ADTTorchHolderComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Ancient)
        {
            _popup.PopupEntity(Loc.GetString("adt-torch-holder-ancient"), ent.Owner, args.User);
            return;
        }

        if (GetTorch(ent) is not { } torch)
        {
            _popup.PopupEntity(Loc.GetString("adt-torch-holder-empty"), ent.Owner, args.User);
            return;
        }

        if (ent.Comp.Status == ADTTorchHolderStatus.Lit && !IsProtected(args.User))
        {
            _popup.PopupEntity(Loc.GetString("adt-torch-holder-too-hot"), ent.Owner, args.User, PopupType.MediumCaution);
            _damageable.TryChangeDamage(args.User, ent.Comp.BurnDamage, true, origin: ent.Owner);
            return;
        }

        if (!_itemSlots.TryEject(ent.Owner, ent.Comp.Slot, args.User, out var ejected))
            return;

        if (ejected != null)
            _hands.PickupOrDrop(args.User, ejected.Value);

        _popup.PopupEntity(Loc.GetString("adt-torch-holder-taken"), ent.Owner, args.User);
    }

    private void OnExamined(Entity<ADTTorchHolderComponent> ent, ref ExaminedEvent args)
    {
        var key = ent.Comp.Status switch
        {
            ADTTorchHolderStatus.Lit => "adt-torch-holder-examine-lit",
            ADTTorchHolderStatus.Unlit => "adt-torch-holder-examine-unlit",
            ADTTorchHolderStatus.Burned => "adt-torch-holder-examine-burned",
            _ => "adt-torch-holder-examine-empty",
        };

        args.PushMarkup(Loc.GetString(key));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<ADTTorchHolderComponent>();

        while (query.MoveNext(out var uid, out var holder))
        {
            if (holder.Ancient || holder.Status is ADTTorchHolderStatus.Empty or ADTTorchHolderStatus.Burned)
                continue;

            UpdateStatus((uid, holder));
        }
    }

    private void UpdateStatus(Entity<ADTTorchHolderComponent> ent)
    {
        var status = ADTTorchHolderStatus.Empty;

        if (ent.Comp.Ancient)
        {
            status = ADTTorchHolderStatus.Lit;
        }
        else if (GetTorch(ent) is { } torch && TryComp<ExpendableLightComponent>(torch, out var light))
        {
            status = light.CurrentState switch
            {
                ExpendableLightState.Lit or ExpendableLightState.Fading => ADTTorchHolderStatus.Lit,
                ExpendableLightState.Dead => ADTTorchHolderStatus.Burned,
                _ => ADTTorchHolderStatus.Unlit,
            };
        }

        if (ent.Comp.Status == status)
            return;

        ent.Comp.Status = status;
        Dirty(ent);

        _light.SetEnabled(ent.Owner, status == ADTTorchHolderStatus.Lit);
        _appearance.SetData(ent.Owner, ADTTorchHolderVisuals.Status, status);
    }

    private EntityUid? GetTorch(Entity<ADTTorchHolderComponent> ent)
    {
        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.Slot, out var slot))
            return null;

        return slot.Item;
    }

    private bool IsProtected(EntityUid user)
    {
        if (HasComp<AshStormImmuneComponent>(user))
            return true;

        if (!_inventory.TryGetSlotEntity(user, "gloves", out var gloves))
            return false;

        return HasComp<TemperatureProtectionComponent>(gloves);
    }
}
