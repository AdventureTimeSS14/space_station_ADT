using Content.Shared.ADT.AshWalker;
using Content.Shared.ADT.Rituals;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Rituals;

public sealed class ADTAshSigilSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTAshSigilComponent, ADTHealTouchUsedEvent>(OnHealTouch);
    }

    private void OnHealTouch(Entity<ADTAshSigilComponent> ent, ref ADTHealTouchUsedEvent args)
    {
        if (args.Handled || ent.Comp.Transforming)
            return;

        ent.Comp.Transforming = true;
        ent.Comp.ActivateAt = _timing.CurTime + ent.Comp.ActivationDelay;
        Dirty(ent);
        args.Handled = true;

        _popup.PopupEntity(
            Loc.GetString("adt-ash-sigil-touched", ("user", args.User)),
            ent.Owner,
            PopupType.Medium);

        LightMarks(ent);

        if (ent.Comp.ActivationEffect is { } effect)
            Spawn(effect, Transform(ent.Owner).Coordinates);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTAshSigilComponent>();

        while (query.MoveNext(out var uid, out var sigil))
        {
            if (sigil.ActivateAt is not { } at || now < at)
                continue;

            Spawn(sigil.Rune, Transform(uid).Coordinates);
            QueueDel(uid);
        }
    }

    private void LightMarks(Entity<ADTAshSigilComponent> ent)
    {
        var marks = new HashSet<Entity<ADTAshRuneMarkComponent>>();
        _lookup.GetEntitiesInRange(Transform(ent.Owner).Coordinates, ent.Comp.MarkRange, marks);

        foreach (var mark in marks)
        {
            if (mark.Comp.Lit)
                continue;

            mark.Comp.Lit = true;
            Dirty(mark);
        }
    }
}
