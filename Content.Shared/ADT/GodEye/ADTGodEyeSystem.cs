using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared.ADT.GodEye;

public sealed class ADTGodEyeSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ClothingSystem _clothing = default!;
    [Dependency] private SharedEyeSystem _eye = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTGodEyeComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<ADTGodEyeComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<ADTGodEyeComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnEquipped(Entity<ADTGodEyeComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!HasComp<UnremoveableComponent>(ent))
        {
            var unremoveable = EnsureComp<UnremoveableComponent>(ent);
            unremoveable.DeleteOnDrop = false;
            Dirty(ent, unremoveable);
        }

        if (HasComp<ADTGodEyeVisionComponent>(args.Wearer))
            return;

        var vision = EnsureComp<ADTGodEyeVisionComponent>(args.Wearer);

        if (!TryComp<EyeComponent>(args.Wearer, out var eye))
            return;

        vision.PreviousDrawFov = eye.DrawFov;
        _eye.SetDrawFov(args.Wearer, false, eye);
    }

    private void OnUnequipped(Entity<ADTGodEyeComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (!TryComp<ADTGodEyeVisionComponent>(args.Wearer, out var vision))
            return;

        _eye.SetDrawFov(args.Wearer, vision.PreviousDrawFov);
        RemComp<ADTGodEyeVisionComponent>(args.Wearer);
    }

    private void OnInteractUsing(Entity<ADTGodEyeComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.Used == ent.Owner || !HasComp<ADTGodEyeComponent>(args.Used))
            return;

        args.Handled = true;

        if (ent.Comp.Double)
        {
            _popup.PopupClient(Loc.GetString("adt-god-eye-vanish"), args.User, args.User);
        }
        else
        {
            ent.Comp.Double = true;
            Dirty(ent);

            _appearance.SetData(ent, ADTGodEyeVisuals.Double, true);
            _clothing.SetEquippedPrefix(ent, ent.Comp.DoublePrefix);
            _meta.SetEntityDescription(ent, Loc.GetString(ent.Comp.DoubleDescription));
        }

        if (_net.IsServer)
            QueueDel(args.Used);
    }
}
