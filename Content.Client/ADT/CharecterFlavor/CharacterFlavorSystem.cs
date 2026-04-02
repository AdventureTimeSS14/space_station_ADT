// Inspired by Nyanotrasen
using System.Collections.Concurrent;
using System.Linq;
using Content.Shared.ADT.CharecterFlavor;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.ADT.CharecterFlavor;

public sealed class CharecterFlavorSystem : SharedCharecterFlavorSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    /// <summary>
    /// Кэш последних запрошенных URL для предотвращения дублирования запросов (для лобби)
    /// </summary>
    private readonly ConcurrentDictionary<string, TimeSpan> _lastRequestTime = new();
    private TimeSpan _lastRequestCheckTime;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SetHeadshotUiMessage>(OnSetHeadshot);
        SubscribeNetworkEvent<HeadshotPreviewEvent>(OnHeadshotPreview);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Очистка старых записей кэша (старше 1 минуты) - только для лобби
        var now = _timing.RealTime;
        if (now - _lastRequestCheckTime > TimeSpan.FromSeconds(10))
        {
            _lastRequestCheckTime = now;
            var expiry = now - TimeSpan.FromMinutes(1);

            // Оптимизированная очистка без ToList() - используем enumerator
            foreach (var kvp in _lastRequestTime)
            {
                if (kvp.Value < expiry)
                {
                    _lastRequestTime.TryRemove(kvp.Key, out _);
                }
            }
        }
    }

    private void OnSetHeadshot(SetHeadshotUiMessage args)
    {
        var controller = _ui.GetUIController<CharacterFlavorUiController>();
        controller.SetHeadshot(args.Target, args.Image);
    }

    private void OnHeadshotPreview(HeadshotPreviewEvent ev)
    {
        var controller = _ui.GetUIController<CharacterFlavorUiController>();
        controller.SetPreviewHeadshot(ev.Image);
    }

    /// <summary>
    /// Запросить у сервера предпросмотр хэдшота по URL (используется в лобби).
    /// </summary>
    public void RequestHeadshotPreview(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        // Предварительная валидация формата URL перед отправкой на сервер
        if (!IsValidHeadshotUrlFormat(url))
        {
            Logger.Debug($"Invalid headshot URL format (client-side): {url}");
            return;
        }

        // Проверка на дублирование запросов (не чаще 1 секунды на один URL) - только для лобби
        var now = _timing.RealTime;
        if (_lastRequestTime.TryGetValue(url, out var lastTime) && now - lastTime < TimeSpan.FromSeconds(1))
        {
            Logger.Debug($"Headshot request skipped (duplicate): {url}");
            return;
        }

        _lastRequestTime[url] = now;
        RaiseNetworkEvent(new RequestHeadshotPreviewEvent(url));
    }

    protected override void OpenFlavor(EntityUid actor, EntityUid target)
    {
        base.OpenFlavor(actor, target);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (!HasComp<CharacterFlavorComponent>(target))
            return;

        var controller = _ui.GetUIController<CharacterFlavorUiController>();
        controller.OpenMenu(target);
    }
}
