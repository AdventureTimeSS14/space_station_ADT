using System.Linq;
using Content.Server.Body.Systems;
using Content.Shared.ADT.Body.Allergies;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects.Effects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Body;

public sealed partial class AllergicSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private ProtoId<DamageGroupPrototype> _allergyDamageGroup = "Poison";
    private DamageTypePrototype? _allergyDamageTypeProto;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AllergicComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AllergicComponent, GetReagentEffectsEvent>(OnGetReagentEffects);

        SubscribeLocalEvent<AllergicComponent, AllergyTriggeredEvent>(OnAllergyTriggered);

        InitializeShock();

        _allergyDamageTypeProto = _proto.Index<DamageTypePrototype>(_allergyDamageGroup);
    }

    public void OnInit(EntityUid uid, AllergicComponent component, ComponentInit args)
    {
        component.Triggers = GetRandomAllergies(component.Min, component.Max);
    }

    public void OnGetReagentEffects(EntityUid uid, AllergicComponent component, ref GetReagentEffectsEvent ev)
    {
        if (!component.Triggers.Contains(ev.Reagent.Prototype))
            return;

        var damageSpecifier = new DamageSpecifier(_allergyDamageTypeProto!, 0.25);
        var damageEffect = new HealthChange
        {
            Damage = damageSpecifier
        };

        AllergyTriggeredEvent triggered = new();
        RaiseLocalEvent(uid, ref triggered);

        Log.Debug("We got some nasty shit inside!");

        ev.Effects = ev.Effects.Append(damageEffect).ToArray();
    }

    private void OnAllergyTriggered(EntityUid uid, AllergicComponent allergic, ref AllergyTriggeredEvent ev)
    {
        Log.Debug("We got trigger event");
        ShockOnTrigger(uid, allergic, ref ev);
        IncrementStackOnTrigger(uid, allergic, ref ev);
    }

    private List<ProtoId<ReagentPrototype>> GetRandomAllergies(int min, int max)
    {
        int reagentsCount = _proto.Count<ReagentPrototype>();

        if (reagentsCount == 0)
            return new();

        int safeMin = Math.Clamp(min, 0, reagentsCount);
        int safeMax = Math.Clamp(max, safeMin, reagentsCount);
        int allergiesCount = _random.Next(safeMin, safeMax + 1);

        var picked = new HashSet<int>();
        while (picked.Count < allergiesCount)
        {
            picked.Add(_random.Next(reagentsCount));
        }

        int index = 0;
        List<ProtoId<ReagentPrototype>> allergies = new();

        foreach (var proto in _proto.EnumeratePrototypes<ReagentPrototype>())
        {
            if (picked.Contains(index))
                allergies.Add(proto.ID);
            index++;
        }

        return allergies;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AllergicComponent>();

        while (query.MoveNext(out var uid, out var allergic))
        {
            UpdateStack(uid, allergic);
            UpdateShock(uid, allergic);
        }
    }
}
