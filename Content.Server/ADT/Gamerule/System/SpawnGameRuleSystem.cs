using Content.Server.GameTicking;
using Content.Shared.ADT.GameRules.Components;
using Robust.Shared.GameStates;

namespace Content.Server.ADT.GameRules;

public sealed class SpawnGameRuleSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;

    // Флаг для отслеживания уже обработанных сущностей
    private readonly HashSet<EntityUid> _processedEntities = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnGameRuleComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SpawnGameRuleComponent, MapInitEvent>(OnMapInit);
    }

    private void OnComponentStartup(EntityUid uid, SpawnGameRuleComponent component, ComponentStartup args)
    {
        if (_processedEntities.Contains(uid))
            return;

        TryStartGameRule(uid, component);
        _processedEntities.Add(uid);
    }

    private void OnMapInit(EntityUid uid, SpawnGameRuleComponent component, MapInitEvent args)
    {
        if (_processedEntities.Contains(uid))
            return;

        TryStartGameRule(uid, component);
        _processedEntities.Add(uid);
    }

    private void TryStartGameRule(EntityUid uid, SpawnGameRuleComponent component)
    {
        if (string.IsNullOrEmpty(component.GameRuleProto))
            return;

        // Добавляем игровое правило через GameTicker
        var ruleEntity = _gameTicker.AddGameRule(component.GameRuleProto);

        // Если нужно сразу запустить правило
        if (component.StartImmediately)
        {
            _gameTicker.StartGameRule(ruleEntity);
        }

        // Опционально: удаляем компонент после использования
        // RemComp<SpawnGameRuleComponent>(uid);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _processedEntities.Clear();
    }
}