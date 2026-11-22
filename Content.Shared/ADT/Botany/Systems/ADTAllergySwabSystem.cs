using System.Linq;
using Content.Shared.ADT.Body.Allergies;
using Content.Shared.ADT.Botany.Components;
using Content.Shared.Body;
using Content.Shared.Botany.Items.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Botany.Systems;

public sealed class ADTAllergySwabSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTAllergySwabComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ADTAllergySwabComponent, ADTAllergySwabDoAfterEvent>(OnDoAfter);
    }

    public bool IsUsed(Entity<ADTAllergySwabComponent?> ent)
    {
        return Resolve(ent, ref ent.Comp, false) && ent.Comp.AllergicTriggers != null;
    }

    private void OnAfterInteract(Entity<ADTAllergySwabComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach || !HasComp<BodyComponent>(target))
            return;

        TryComp<BotanySwabComponent>(ent, out var swab);
        if (swab?.PlantData != null)
        {
            _popup.PopupClient(Loc.GetString("botany-swab-unusable-bio"), args.User, args.User);
            return;
        }

        var delay = swab?.SwabDelay ?? TimeSpan.FromSeconds(2);
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, delay, new ADTAllergySwabDoAfterEvent(), ent, target: target, used: ent)
        {
            BreakOnMove = true,
            NeedHand = true,
        });
        args.Handled = true;
    }

    private void OnDoAfter(Entity<ADTAllergySwabComponent> ent, ref ADTAllergySwabDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is not { } target)
            return;

        if (TryComp<AllergicComponent>(target, out var allergic))
        {
            ent.Comp.AllergicTriggers = ent.Comp.AllergicTriggers == null
                ? allergic.Triggers.ToList()
                : ent.Comp.AllergicTriggers.Concat(allergic.Triggers).Distinct().ToList();
        }

        ent.Comp.AllergicTriggers ??= new();
        Dirty(ent);

        _popup.PopupClient(Loc.GetString("botany-swab-used-bio"), args.Args.User, args.Args.User);
        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class ADTAllergySwabDoAfterEvent : SimpleDoAfterEvent;
