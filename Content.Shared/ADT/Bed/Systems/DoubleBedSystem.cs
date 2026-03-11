using Content.Shared.ADT.Bed.Components;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Interaction;
using Content.Shared.Placeable;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using System.Linq;
using System.Numerics;

namespace Content.Shared.ADT.Bed;

/// <summary>
/// Система для управления двуспальными кроватями с поддержкой двух позиций для пристёгивания и размещения постельного белья.
/// </summary>
public sealed class DoubleBedSystem : EntitySystem
{
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly PlaceableSurfaceSystem _placeableSurface = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DoubleBedComponent, ComponentStartup>(OnDoubleBedStartup);
        SubscribeLocalEvent<DoubleBedComponent, StrapAttemptEvent>(OnStrapAttempt, before: new[] { typeof(SharedBuckleSystem) });
        SubscribeLocalEvent<DoubleBedComponent, UnstrappedEvent>(OnUnstrapped, after: new[] { typeof(SharedBuckleSystem) });
        SubscribeLocalEvent<DoubleBedComponent, AfterInteractUsingEvent>(OnAfterInteractUsing, before: new[] { typeof(PlaceableSurfaceSystem) });
    }

    private void OnDoubleBedStartup(Entity<DoubleBedComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<StrapComponent>(ent, out var strap))
        {
            if (strap.BuckleOffset == Vector2.Zero)
            {
                strap.BuckleOffset = ent.Comp.LeftOffset;
                Dirty(ent, strap);
            }
        }
    }

    private void OnStrapAttempt(Entity<DoubleBedComponent> ent, ref StrapAttemptEvent args)
    {
        if (!TryComp<StrapComponent>(ent, out var strap))
            return;

        var offset = strap.BuckledEntities.Count == 0 ? ent.Comp.LeftOffset : ent.Comp.RightOffset;
        strap.BuckleOffset = offset;
        Dirty(ent, strap);
    }

    private void OnUnstrapped(Entity<DoubleBedComponent> ent, ref UnstrappedEvent args)
    {
        if (!TryComp<StrapComponent>(ent, out var strap))
            return;

        strap.BuckleOffset = ent.Comp.LeftOffset;
        Dirty(ent, strap);
    }

    private void OnAfterInteractUsing(Entity<DoubleBedComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!TryComp<PlaceableSurfaceComponent>(ent, out var surface))
            return;

        const string bedsheetTag = "Bedsheet";
        if (!_tagSystem.HasTag(args.Used, bedsheetTag))
            return;

        var bedCoords = Transform(ent).Coordinates;
        var bedsheetCount = 0;

        var query = EntityQueryEnumerator<TagComponent>();
        while (query.MoveNext(out var uid, out var tagComp))
        {
            if (uid == args.Used)
                continue;

            if (!_tagSystem.HasTag(uid, bedsheetTag))
                continue;

            var bedsheetCoords = Transform(uid).Coordinates;
            if (bedsheetCoords.TryDistance(EntityManager, bedCoords, out var distance) && distance < 0.5f)
                bedsheetCount++;
        }

        var offset = bedsheetCount == 0 ? ent.Comp.RightBedsheetOffset : ent.Comp.LeftBedsheetOffset;

        _placeableSurface.SetPositionOffset(ent, offset, surface);
    }
}
