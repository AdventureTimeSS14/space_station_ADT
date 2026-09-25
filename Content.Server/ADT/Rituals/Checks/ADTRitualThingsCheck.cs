using Content.Shared.ADT.Rituals;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.Server.ADT.Rituals.Checks;

public sealed partial class ADTRitualThingsCheck : ADTRitualCheck
{
    [DataField]
    public bool MustBeDead;

    [DataField]
    public bool MustBeAlive;

    [DataField]
    public bool MustHaveMind;

    [DataField]
    public bool MustLackMind;

    [DataField]
    public bool NoMegafauna;

    [DataField(required: true)]
    public LocId Reason = default!;

    public override bool Check(IEntityManager entMan, ADTRitualArgs args, out string? reason)
    {
        reason = Reason;

        var mobState = entMan.System<MobStateSystem>();

        foreach (var thing in args.UsedThings)
        {
            if (!entMan.HasComponent<MobStateComponent>(thing))
                continue;

            if (MustBeDead && !mobState.IsDead(thing))
                return false;

            if (MustBeAlive && mobState.IsDead(thing))
                return false;

            var hasMind = entMan.TryGetComponent<MindContainerComponent>(thing, out var mind) && mind.HasMind;

            if (MustHaveMind && !hasMind)
                return false;

            if (MustLackMind && hasMind)
                return false;

            if (NoMegafauna && entMan.HasComponent<MegafaunaComponent>(thing))
                return false;
        }

        reason = null;
        return true;
    }
}
