using Content.Shared.ADT.Bubblegum.Loot;
using Content.Shared.Clothing;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Bubblegum;

public sealed class DemonVisionSystem : VisualizerSystem<DemonVisionedComponent>
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private const string DemonLayerKey = "ADTDemonVision";
    private static readonly ResPath DemonRsi = new ResPath("ADT/Effects/bubblegum_demon.rsi");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodFrenzyComponent, ComponentStartup>(OnFrenzyStart);
        SubscribeLocalEvent<BloodFrenzyComponent, ComponentShutdown>(OnFrenzyEnd);
        SubscribeLocalEvent<HumanoidProfileComponent, ComponentStartup>(OnHumanoidStartup);

        SubscribeLocalEvent<DemonVisionedComponent, ComponentStartup>(OnVisionStartup);
        SubscribeLocalEvent<DemonVisionedComponent, ComponentShutdown>(OnVisionShutdown);

        SubscribeLocalEvent<EquipmentVisualsUpdatedEvent>(OnEquipmentUpdated);
        SubscribeLocalEvent<HeldVisualsUpdatedEvent>(OnHeldUpdated);
    }

    private bool LocalFrenzied()
    {
        var local = _player.LocalEntity;
        return local != null && HasComp<BloodFrenzyComponent>(local.Value);
    }

    private void OnFrenzyStart(Entity<BloodFrenzyComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        var query = EntityQueryEnumerator<HumanoidProfileComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (uid == ent.Owner)
                continue;

            EnsureComp<DemonVisionedComponent>(uid);
        }
    }

    private void OnFrenzyEnd(Entity<BloodFrenzyComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        var query = EntityQueryEnumerator<DemonVisionedComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            RemCompDeferred<DemonVisionedComponent>(uid);
        }
    }

    private void OnHumanoidStartup(Entity<HumanoidProfileComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity == ent.Owner || !LocalFrenzied())
            return;

        EnsureComp<DemonVisionedComponent>(ent.Owner);
    }

    private void OnVisionStartup(Entity<DemonVisionedComponent> ent, ref ComponentStartup args)
    {
        ApplyDemon(ent.Owner);
    }

    private void OnVisionShutdown(Entity<DemonVisionedComponent> ent, ref ComponentShutdown args)
    {
        RestoreSprite(ent.Owner);
    }

    private void OnEquipmentUpdated(EquipmentVisualsUpdatedEvent args)
    {
        if (HasComp<DemonVisionedComponent>(args.Equipee))
            ApplyDemon(args.Equipee);
    }

    private void OnHeldUpdated(HeldVisualsUpdatedEvent args)
    {
        if (HasComp<DemonVisionedComponent>(args.User))
            ApplyDemon(args.User);
    }

    protected override void OnAppearanceChange(EntityUid uid, DemonVisionedComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        ApplyDemon(uid, args.Sprite);
    }

    private void ApplyDemon(EntityUid uid)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
            ApplyDemon(uid, sprite);
    }

    private void ApplyDemon(EntityUid uid, SpriteComponent sprite)
    {
        if (!SpriteSystem.LayerExists((uid, sprite), DemonLayerKey))
        {
            var index = SpriteSystem.AddLayer((uid, sprite), new SpriteSpecifier.Rsi(DemonRsi, "icon"));
            SpriteSystem.LayerMapSet((uid, sprite), DemonLayerKey, index);
        }

        var i = 0;
        while (SpriteSystem.LayerExists((uid, sprite), i))
        {
            SpriteSystem.LayerSetVisible((uid, sprite), i, false);
            i++;
        }

        SpriteSystem.LayerSetVisible((uid, sprite), DemonLayerKey, true);
    }

    private void RestoreSprite(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        SpriteSystem.RemoveLayer((uid, sprite), DemonLayerKey, false);

        var i = 0;
        while (SpriteSystem.LayerExists((uid, sprite), i))
        {
            SpriteSystem.LayerSetVisible((uid, sprite), i, true);
            i++;
        }

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            AppearanceSystem.QueueUpdate(uid, appearance);
    }
}
