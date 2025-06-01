using Content.Shared.ADT.PaperOrigami;
using Content.Shared.ADT.PaperOrigami.Components;
using Content.Shared.Paper;
using Robust.Client.GameObjects;

namespace Content.Client.ADT.PaperOrigami;

public sealed class PaperOrigamiSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PaperOrigamiComponent, AppearanceChangeEvent>(ChangeAppearanceSprite);
        SubscribeLocalEvent<PaperOrigamiComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, PaperOrigamiComponent component, ComponentStartup args)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.LayerSetVisible(0, true);  // Основной спрайт бумаги виден
            sprite.LayerSetVisible(1, false); // Слой текста скрыт
            sprite.LayerSetVisible(2, false); // Слой штампа скрыт
            sprite.LayerSetVisible(3, false); // Слой оригами скрыт
        }
        else
        {
            Logger.Error($"SpriteComponent отсутствует для сущности {uid}. PaperOrigami визуализация не инициализирована.");
            return;
        }
    }

    public void ChangeAppearanceSprite(EntityUid uid, PaperOrigamiComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (_appearance.TryGetData<bool>(uid, PaperOrigamiState.State, out var isOrigami))
        {
            sprite.LayerSetVisible(0, !isOrigami); // Слой 0 (бумага) виден, когда не оригами
            sprite.LayerSetVisible(3, isOrigami);  // Слой 3 (оригами) виден, когда оригами

            // Синхронизация с PaperVisualizerSystem для слоёв текста и штампа
            bool showWriting = _appearance.TryGetData<PaperComponent.PaperStatus>(uid, PaperComponent.PaperVisuals.Status, out var writingStatus) && writingStatus == PaperComponent.PaperStatus.Written;
            bool showStamp = _appearance.TryGetData<string>(uid, PaperComponent.PaperVisuals.Stamp, out var stampState) && !string.IsNullOrEmpty(stampState);

            sprite.LayerSetVisible(1, !isOrigami && showWriting); // Текст виден только в состоянии бумаги, если есть текст
            sprite.LayerSetVisible(2, !isOrigami && showStamp);   // Штамп виден только в состоянии бумаги, если есть штамп

            if (showStamp && !isOrigami)
            {
                sprite.LayerSetState(2, stampState); // штампдоывдлтвалд
            }
        }
    }
}