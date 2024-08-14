using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Humanoid;
using Content.Server.Preferences.Managers;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules;

public sealed class AntagSpawnEntityRuleSystem : GameRuleSystem<AntagSpawnEntityRuleComponent>
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AntagSpawnEntityRuleComponent, AntagSelectEntityEvent>(OnSelectEntity);
    }

    private void OnSelectEntity(Entity<AntagSpawnEntityRuleComponent> ent, ref AntagSelectEntityEvent args)
    {
        if (args.Handled)
            return;

        args.Entity = Spawn(ent.Comp.Prototype);
    }
}
