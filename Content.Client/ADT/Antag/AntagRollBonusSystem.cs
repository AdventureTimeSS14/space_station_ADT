using Content.Shared.ADT.Antag;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Antag;

public sealed class AntagRollBonusSystem : EntitySystem
{
    private Dictionary<ProtoId<AntagPrototype>, float> _bonuses = new();

    public event Action? OnBonusesUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AntagRollBonusInfoEvent>(OnInfoReceived);
    }

    public void RequestUpdate()
    {
        RaiseNetworkEvent(new RequestAntagRollBonusInfoEvent());
    }

    public float? GetBonusPercent(ProtoId<AntagPrototype> role)
    {
        if (!_bonuses.TryGetValue(role, out var weight))
            return null;

        return (weight - 1f) * 100f;
    }

    private void OnInfoReceived(AntagRollBonusInfoEvent ev)
    {
        _bonuses = ev.Bonuses;
        OnBonusesUpdated?.Invoke();
    }
}
