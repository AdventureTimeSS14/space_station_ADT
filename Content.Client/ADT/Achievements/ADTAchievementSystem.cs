using Content.Shared.ADT.Achievements;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Achievements;

public sealed class ADTAchievementSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private readonly Dictionary<string, ADTAchievementState> _states = new();

    public event Action? Updated;

    public event Action<ADTAchievementPrototype>? Unlocked;

    public IReadOnlyDictionary<string, ADTAchievementState> States => _states;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ADTAchievementsStateEvent>(OnState);
        SubscribeNetworkEvent<ADTAchievementUpdateEvent>(OnUpdate);
    }

    public void RequestState()
    {
        RaiseNetworkEvent(new ADTAchievementsRequestEvent());
    }

    public ADTAchievementState GetState(string achievement)
    {
        return _states.GetValueOrDefault(achievement);
    }

    private void OnState(ADTAchievementsStateEvent args)
    {
        _states.Clear();

        foreach (var (id, state) in args.Achievements)
        {
            _states[id] = state;
        }

        Updated?.Invoke();
    }

    private void OnUpdate(ADTAchievementUpdateEvent args)
    {
        _states[args.Achievement] = args.State;
        Updated?.Invoke();

        if (!args.Announce)
            return;

        if (_prototype.TryIndex<ADTAchievementPrototype>(args.Achievement, out var proto))
            Unlocked?.Invoke(proto);
    }
}
