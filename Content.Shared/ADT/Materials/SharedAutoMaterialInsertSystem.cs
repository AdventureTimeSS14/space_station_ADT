using Content.Shared.Interaction;
using Content.Shared.Tag;
using Content.Shared.Storage;
using Content.Shared.Materials;
using Content.Shared.Popups;

namespace Content.Shared.ADT.Materials;

public abstract class SharedAutoMaterialInsertSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoMaterialInsertComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(EntityUid uid, AutoMaterialInsertComponent comp, InteractUsingEvent args)
    {
        var used = args.Used;

        if (!_tag.HasTag(used, comp.Tag))
            return;
        if (!TryComp<StorageComponent>(used, out var storage))
            return;

        foreach (var (item, _) in storage.StoredItems)
        {
            _materialStorage.TryInsertMaterialEntity(args.User, item, uid, showPopup: false);
            _popup.PopupPredicted(Loc.GetString("machine-insert-all", ("user", args.User), ("machine", uid),
                ("item", used)), uid, args.User);
        }
    }
}
