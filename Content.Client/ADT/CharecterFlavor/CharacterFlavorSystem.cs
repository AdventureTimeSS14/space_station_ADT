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
    /// Кэш последних запрошенных URL для предотвращения дублирования запросов
    /// </summary>
    private readonly ConcurrentDictionary<string, TimeSpan> _lastRequestTime = new();
    private TimeSpan _lastRequestCheckTime;

    /// <summary>
    /// Кэш хэшей URL для проверки уже загруженных изображений
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> _urlCache = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SetHeadshotUiMessage>(OnSetHeadshot);
        SubscribeNetworkEvent<HeadshotPreviewEvent>(OnHeadshotPreview);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Очистка старых записей кэша (старше 1 минуты)
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

            // Очистка кэша URL
            _urlCache.Clear();
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

        // Кэшируем факт успешной загрузки (по URL будет проверено при следующем запросе)
        // Для упрощения просто очищаем последний запрос из таймера
        if (_lastRequestTime.Count > 0)
        {
            var lastKey = _lastRequestTime.Keys.Last();
            _urlCache[lastKey] = true;
            _lastRequestTime.TryRemove(lastKey, out _);
        }
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

        // Проверка: если URL уже был успешно загружен, не запрашиваем повторно
        if (_urlCache.ContainsKey(url))
        {
            Logger.Debug($"Headshot already cached, skipping request: {url}");
            return;
        }

        // Проверка на дублирование запросов (не чаще 1 секунды на один URL)
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
