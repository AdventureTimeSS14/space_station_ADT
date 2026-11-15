// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Server.Mind;
using Content.Server.Roles;
using Content.Shared.HealthExaminable;
using Content.Shared.Roles.Components;
using Content.Shared.Surgery.Components;
using Content.Shared.Vampire.Components;
using Robust.Shared.Timing;

namespace Content.Server.Vampire;

/// <summary>
/// Medics can see bite on your neck.
/// </summary>
public sealed partial class BittenByVampireSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private RoleSystem _roles = default!;

    private static readonly HashSet<string> SurgicalJobs = new()
    {
        "ChiefMedicalOfficer",
        "MedicalDoctor",
        "Paramedic",
        "SeniorPhysician",
        "Brigmedic",
        "Detective",
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BittenByVampireComponent, ComponentAdd>(OnComponentAdd);
        SubscribeLocalEvent<BittenByVampireComponent, HealthBeingExaminedEvent>(OnHealthBeingExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _timing.CurTime;

        var query = EntityQueryEnumerator<BittenByVampireComponent>();
        while (query.MoveNext(out var uid, out var bitten))
        {
            if (currentTime >= bitten.ExpirationTime)
                RemComp<BittenByVampireComponent>(uid);
        }
    }

    private void OnComponentAdd(EntityUid uid, BittenByVampireComponent component, ComponentAdd args)
    {
        component.ExpirationTime = _timing.CurTime + TimeSpan.FromSeconds(component.LifetimeSeconds);
    }

    private void OnHealthBeingExamined(Entity<BittenByVampireComponent> ent, ref HealthBeingExaminedEvent args)
    {
        if (HasComp<SurgicalSkillComponent>(args.Examiner) || HasSurgicalJob(args.Examiner))
        {
            args.Message.PushNewline();
            args.Message.AddMarkupOrThrow(Loc.GetString("vampire-bittenbyvampire-examine"));
        }
    }

    private bool HasSurgicalJob(EntityUid examiner)
    {
        return _mind.TryGetMind(examiner, out var mindId, out var mind)
            && _roles.MindHasRole<JobRoleComponent>((mindId, mind), out var jobRole)
            && jobRole.Value.Comp1.JobPrototype is { } job
            && SurgicalJobs.Contains(job);
    }
}
