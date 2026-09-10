using Content.Shared.Cloning.Events;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Server.ADT.Cloning;

public sealed class ADTCloneProfileSystem : EntitySystem
{
    [Dependency] private readonly HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private readonly GrammarSystem _grammar = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, CloningEvent>(OnCloning);
    }

    private void OnCloning(Entity<HumanoidProfileComponent> ent, ref CloningEvent args)
    {
        if (!TryComp<HumanoidProfileComponent>(args.CloneUid, out var cloneProfile))
            return;

        var clone = new Entity<HumanoidProfileComponent?>(args.CloneUid, cloneProfile);

        _humanoidProfile.SetSex(clone, ent.Comp.Sex);
        _humanoidProfile.SetGender(clone, ent.Comp.Gender);
        _humanoidProfile.SetAge(clone, ent.Comp.Age);

        if (TryComp<GrammarComponent>(args.CloneUid, out var grammar))
            _grammar.SetGender((args.CloneUid, grammar), ent.Comp.Gender);

        _identity.QueueIdentityUpdate(args.CloneUid);
    }
}
