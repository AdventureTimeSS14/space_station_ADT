using Content.Shared.Examine;
using Content.Shared.Movement.Systems;

namespace Content.Shared.ADT.Nutrition;

public sealed class ADTFatSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTFatComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<ADTFatComponent, ExaminedEvent>(OnExamined);
    }

    private void OnRefreshMovespeed(EntityUid uid, ADTFatComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.SpeedModifier, component.SpeedModifier);
    }

    private void OnExamined(Entity<ADTFatComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("fat-examine", ("ent", ent.Owner)));
    }
}