using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Cards.Card;
using Content.Shared.ADT.Cards.Hand;
using Content.Shared.ADT.Cards.Stack;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Containers;

namespace Content.Client.ADT.Cards.Hand;

/// <summary>
/// This handles...
/// </summary>
public sealed class CardHandSystem : EntitySystem
{
    private readonly Dictionary<Entity<CardHandComponent>, int> _notInit = [];
    [Dependency] private CardSpriteSystem _cardSpriteSystem = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private IPlayerManager _player = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<CardHandComponent, ComponentStartup>(OnComponentStartupEvent);
        SubscribeNetworkEvent<CardStackInitiatedEvent>(OnStackStart);
        SubscribeNetworkEvent<CardStackQuantityChangeEvent>(OnStackUpdate);
        SubscribeNetworkEvent<CardStackReorderedEvent>(OnStackReorder);
        SubscribeNetworkEvent<CardStackFlippedEvent>(OnStackFlip);
        SubscribeLocalEvent<CardHandComponent, EntGotInsertedIntoContainerMessage>(OnContainerChanged);
        SubscribeLocalEvent<CardHandComponent, EntGotRemovedFromContainerMessage>(OnContainerChanged);
        _player.LocalPlayerAttached += OnLocalPlayerChanged;
        _player.LocalPlayerDetached += OnLocalPlayerChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _player.LocalPlayerAttached -= OnLocalPlayerChanged;
        _player.LocalPlayerDetached -= OnLocalPlayerChanged;
    }

    private void OnLocalPlayerChanged(EntityUid uid)
    {
        var query = AllEntityQuery<CardHandComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            UpdateSprite(ent, comp);
        }
    }

    private bool ShouldHideFaces(EntityUid uid)
    {
        if (!TryComp(uid, out TransformComponent? xform)
            || !_container.TryGetOuterContainer(uid, xform, out var container))
            return false;

        return container.Owner != _player.LocalEntity;
    }

    private void OnContainerChanged<T>(EntityUid uid, CardHandComponent comp, T args)
        => UpdateSprite(uid, comp);

    private void ShowBack(Entity<SpriteComponent> sprite, List<EntityUid>? hiddenCards, int cardIndex, int layerIndex)
    {
        if (hiddenCards is null
            || cardIndex >= hiddenCards.Count
            || !TryComp(hiddenCards[cardIndex], out CardComponent? card)
            || card.BackSprite.Count <= 0)
            return;

        sprite.Comp.LayerSetSprite(layerIndex, card.BackSprite[0]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var (ent, value) in _notInit)
        {
            if (value >= 5)
            {
                _notInit.Remove(ent);
                continue;
            }
            _notInit[ent] = value + 1;
            if (!TryComp(ent.Owner, out CardStackComponent? stack) || stack.Cards.Count <= 0)
                continue;

            // If cards were correctly initialized, we update the sprite
            UpdateSprite(ent.Owner, ent.Comp);
            _notInit.Remove(ent);
        }
    }

    private bool TryGetCardLayer(EntityUid card, out SpriteComponent.Layer? layer)
    {
        layer = null;
        if (!TryComp(card, out SpriteComponent? cardSprite)
            || !cardSprite.TryGetLayer(0, out var l))
            return false;

        layer = l;
        return true;
    }

    private void UpdateSprite(EntityUid uid, CardHandComponent comp)
    {
        if (!TryComp(uid, out SpriteComponent? sprite)
            || !TryComp(uid, out CardStackComponent? cardStack))
            return;

        // Prevents error appearing at spawnMenu
        if (cardStack.Cards.Count <= 0 || !TryGetCardLayer(cardStack.Cards.Last(), out var cardlayer) ||
            cardlayer == null)
        {
            _notInit[(uid, comp)] = 0;
            return;
        }

        _cardSpriteSystem.TryAdjustLayerQuantity((uid, sprite, cardStack), comp.CardLimit);

        var cardCount = Math.Min(cardStack.Cards.Count, comp.CardLimit);
        var hiddenCards = ShouldHideFaces(uid) ? cardStack.Cards.TakeLast(cardCount).ToList() : null;

        // Frontier: zero/one card case
        if (cardCount <= 0)
        {
            // Placeholder - we need to have a valid sprite.
            sprite.LayerSetVisible(0, true);
            sprite.LayerSetState(0, "singlecard_down_black");
            sprite.LayerSetOffset(0, new Vector2(0f, 0f));
            sprite.LayerSetScale(0, new Vector2(1f, 1f));
        }
        else if (cardCount == 1)
        {
            _cardSpriteSystem.TryHandleLayerConfiguration(
                (uid, sprite, cardStack),
                cardCount,
                (sprt, cardIndex, layerIndex) =>
                {
                    ShowBack(sprt, hiddenCards, cardIndex, layerIndex);
                    sprt.Comp.LayerSetRotation(layerIndex, Angle.FromDegrees(0));
                    sprt.Comp.LayerSetOffset(layerIndex, new Vector2(0, 0.10f));
                    sprt.Comp.LayerSetScale(layerIndex, new Vector2(comp.Scale, comp.Scale));
                    return true;
                }
            );
        }
        else
        {
            var intervalAngle = comp.Angle / (cardCount - 1);
            var intervalSize = comp.XOffset / (cardCount - 1);

            _cardSpriteSystem.TryHandleLayerConfiguration(
                (uid, sprite, cardStack),
                cardCount,
                (sprt, cardIndex, layerIndex) =>
                {
                    var angle = (-(comp.Angle / 2)) + cardIndex * intervalAngle;
                    var x = (-(comp.XOffset / 2)) + cardIndex * intervalSize;
                    var y = -(x * x) + 0.10f;

                    ShowBack(sprt, hiddenCards, cardIndex, layerIndex);
                    sprt.Comp.LayerSetRotation(layerIndex, Angle.FromDegrees(-angle));
                    sprt.Comp.LayerSetOffset(layerIndex, new Vector2(x, y));
                    sprt.Comp.LayerSetScale(layerIndex, new Vector2(comp.Scale, comp.Scale));
                    return true;
                }
            );
        }
    }


    private void OnStackUpdate(CardStackQuantityChangeEvent args)
    {
        if (!TryComp(GetEntity(args.Stack), out CardHandComponent? comp))
            return;
        UpdateSprite(GetEntity(args.Stack), comp);
    }

    private void OnStackStart(CardStackInitiatedEvent args)
    {
        var entity = GetEntity(args.CardStack);
        if (!TryComp(entity, out CardHandComponent? comp))
            return;

        UpdateSprite(entity, comp);
    }
    private void OnComponentStartupEvent(EntityUid uid, CardHandComponent comp, ComponentStartup args)
    {
        if (!TryComp(uid, out CardStackComponent? stack))
        {
            _notInit[(uid, comp)] = 0;
            return;
        }
        if (stack.Cards.Count <= 0)
            _notInit[(uid, comp)] = 0;
        UpdateSprite(uid, comp);
    }

    // Frontier
    private void OnStackReorder(CardStackReorderedEvent args)
    {
        if (!TryComp(GetEntity(args.Stack), out CardHandComponent? comp))
            return;
        UpdateSprite(GetEntity(args.Stack), comp);
    }

    private void OnStackFlip(CardStackFlippedEvent args)
    {
        var entity = GetEntity(args.CardStack);
        if (!TryComp(entity, out CardHandComponent? comp))
            return;

        UpdateSprite(entity, comp);
    }
}
