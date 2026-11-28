using System.Numerics;
using Content.Shared.ADT.EatToGrow;
using Content.Shared.Mobs;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Sprite;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Systems;

namespace Content.Server.ADT.EatToGrow;

public sealed class EatToGrowSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EatToGrowComponent, IngestingEvent>(OnIngesting);
        SubscribeLocalEvent<EatToGrowComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnIngesting(Entity<EatToGrowComponent> ent, ref IngestingEvent args)
    {
        if (!TryComp<EdibleComponent>(args.Food, out var edible) || edible.Edible != IngestionSystem.Food)
            return;

        SetScale(ent, MathF.Min(ent.Comp.CurrentScale + ent.Comp.Growth, ent.Comp.MaxGrowth));
    }

    private void OnMobStateChanged(Entity<EatToGrowComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead || !ent.Comp.ShrinkOnDeath)
            return;

        SetScale(ent, 1f);
    }

    private void SetScale(Entity<EatToGrowComponent> ent, float scale)
    {
        var delta = scale - ent.Comp.CurrentScale;
        if (MathHelper.CloseTo(delta, 0f))
            return;

        ent.Comp.CurrentScale = scale;

        EnsureComp<ScaleVisualsComponent>(ent);
        var appearance = EnsureComp<AppearanceComponent>(ent);
        if (!_appearance.TryGetData<Vector2>(ent, ScaleVisuals.Scale, out var oldScale, appearance))
            oldScale = Vector2.One;

        _appearance.SetData(ent, ScaleVisuals.Scale, oldScale + new Vector2(delta, delta), appearance);

        if (!TryComp<FixturesComponent>(ent, out var fixtures))
            return;

        foreach (var (id, fixture) in fixtures.Fixtures)
        {
            if (fixture.Shape is not PhysShapeCircle circle)
                continue;

            _physics.SetPositionRadius(ent, id, fixture, circle, circle.Position, circle.Radius + delta / 4, fixtures);
        }
    }
}
