using Content.Shared.ADT.Lavaland;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Temperature.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTLavalandHumanResistsSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IComponentFactory _compFactory = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLavalandHumanResistsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTLavalandHumanResistsComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<ADTLavalandHumanResistsComponent, MapUidChangedEvent>(OnMapUidChanged);
        SubscribeLocalEvent<ADTLavalandHumanResistsComponent, MetaFlagRemoveAttemptEvent>(OnMetaFlagRemoveAttempt);
    }

    private void OnMapInit(Entity<ADTLavalandHumanResistsComponent> ent, ref MapInitEvent args)
    {
        _meta.AddFlag(ent, MetaDataFlags.ExtraTransformEvents);
        Update(ent, Transform(ent).MapUid);
    }

    private void OnRemove(Entity<ADTLavalandHumanResistsComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.Active && !TerminatingOrDeleted(ent))
            Restore(ent);

        _meta.RemoveFlag(ent, MetaDataFlags.ExtraTransformEvents);
    }

    private void OnMapUidChanged(Entity<ADTLavalandHumanResistsComponent> ent, ref MapUidChangedEvent args)
    {
        Update(ent, args.NewMap);
    }

    private void OnMetaFlagRemoveAttempt(Entity<ADTLavalandHumanResistsComponent> ent, ref MetaFlagRemoveAttemptEvent args)
    {
        if ((args.ToRemove & MetaDataFlags.ExtraTransformEvents) != 0 &&
            ent.Comp.LifeStage <= ComponentLifeStage.Running)
        {
            args.ToRemove &= ~MetaDataFlags.ExtraTransformEvents;
        }
    }

    private void Update(Entity<ADTLavalandHumanResistsComponent> ent, EntityUid? map)
    {
        var onLavaland = map != null && HasComp<ADTLavalandMapComponent>(map);

        if (onLavaland && !ent.Comp.Active && IsRoundStartSpecies(ent))
            Apply(ent);
        else if (!onLavaland && ent.Comp.Active)
            Restore(ent);
    }

    private bool IsRoundStartSpecies(EntityUid uid)
    {
        if (!TryComp<HumanoidProfileComponent>(uid, out var profile))
            return false;

        return _proto.TryIndex(profile.Species, out var species) && species.RoundStart;
    }

    private void Apply(Entity<ADTLavalandHumanResistsComponent> ent)
    {
        if (!_proto.TryIndex(ent.Comp.Reference, out var reference))
            return;

        ent.Comp.Active = true;

        if (TryComp<DamageableComponent>(ent, out var damageable) &&
            reference.TryGetComponent<DamageableComponent>(out var refDamageable, _compFactory))
        {
            ent.Comp.OriginalModifierSet = damageable.DamageModifierSetId;
            _damageable.SetDamageModifierSetId((ent, damageable), refDamageable.DamageModifierSetId);
        }

        if (TryComp<TemperatureDamageComponent>(ent, out var temperature) &&
            reference.TryGetComponent<TemperatureDamageComponent>(out var refTemperature, _compFactory))
        {
            ent.Comp.OriginalHeatThreshold = temperature.HeatDamageThreshold;
            ent.Comp.OriginalColdThreshold = temperature.ColdDamageThreshold;
            ent.Comp.OriginalHeatDamage = temperature.HeatDamage;
            ent.Comp.OriginalColdDamage = temperature.ColdDamage;

            temperature.HeatDamageThreshold = refTemperature.HeatDamageThreshold;
            temperature.ColdDamageThreshold = refTemperature.ColdDamageThreshold;
            temperature.HeatDamage = new(refTemperature.HeatDamage);
            temperature.ColdDamage = new(refTemperature.ColdDamage);
        }
    }

    private void Restore(Entity<ADTLavalandHumanResistsComponent> ent)
    {
        ent.Comp.Active = false;

        if (TryComp<DamageableComponent>(ent, out var damageable))
            _damageable.SetDamageModifierSetId((ent, damageable), ent.Comp.OriginalModifierSet);

        if (TryComp<TemperatureDamageComponent>(ent, out var temperature) &&
            ent.Comp.OriginalHeatDamage != null &&
            ent.Comp.OriginalColdDamage != null)
        {
            temperature.HeatDamageThreshold = ent.Comp.OriginalHeatThreshold;
            temperature.ColdDamageThreshold = ent.Comp.OriginalColdThreshold;
            temperature.HeatDamage = ent.Comp.OriginalHeatDamage;
            temperature.ColdDamage = ent.Comp.OriginalColdDamage;
        }

        ent.Comp.OriginalModifierSet = null;
        ent.Comp.OriginalHeatDamage = null;
        ent.Comp.OriginalColdDamage = null;
    }
}
