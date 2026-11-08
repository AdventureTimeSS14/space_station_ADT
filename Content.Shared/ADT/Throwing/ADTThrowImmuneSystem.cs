namespace Content.Shared.ADT.Throwing;

public sealed class ADTThrowImmuneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTThrowImmuneComponent, ADTBeforeThrownEvent>(OnBeforeThrown);
    }

    private void OnBeforeThrown(Entity<ADTThrowImmuneComponent> ent, ref ADTBeforeThrownEvent args)
    {
        if (!ent.Comp.Bypass)
            args.Cancelled = true;
    }
}
