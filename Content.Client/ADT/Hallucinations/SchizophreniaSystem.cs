using System.Linq;
using System.Numerics;
using Content.Client.Audio;
using Content.Shared.ADT.Shizophrenia;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.ADT.Shizophrenia;

public sealed class SchizophreniaSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ContentAudioSystem _contentAudio = default!;

    private Dictionary<NetEntity, TimeSpan> _layers = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SetHallucinationAppearanceMessage>(OnAppearanceMessage);

        SubscribeLocalEvent<SchizophreniaComponent, GetStatusIconsEvent>(OnGetStatusIcons);

        SubscribeLocalEvent<HallucinationsMusicComponent, MapInitEvent>(OnMusicInit);
        SubscribeLocalEvent<HallucinationsMusicComponent, ComponentShutdown>(OnMusicShutdown);
        SubscribeLocalEvent<HallucinationsMusicComponent, LocalPlayerAttachedEvent>(OnMusicAttach);
        SubscribeLocalEvent<HallucinationsMusicComponent, LocalPlayerDetachedEvent>(OnMusicDetach);
        SubscribeLocalEvent<HallucinationsMusicComponent, AfterAutoHandleStateEvent>(OnMusicHandleState);
    }

    private void OnAppearanceMessage(SetHallucinationAppearanceMessage args)
    {
        if (_player.LocalSession == null)
            return;

        // Get target entity
        var ents = EntityManager.AllEntities<HumanoidAppearanceComponent>().Where(x => x.Owner != _player.LocalEntity && !HasComp<HallucinationComponent>(x)).ToList();
        var selected = _random.Pick(ents);

        var proto = _random.Pick(args.Appearance.Prototypes);
        var item = Spawn(proto);

        if (!TryComp<SpriteComponent>(selected, out var sprite) || !TryComp<SpriteComponent>(item, out var itemSprite))
            return;

        var state = _random.Pick(args.Appearance.States);

        // Ensure that given prototype sprite contains our state
        var rsi = itemSprite.BaseRSI;
        if (rsi == null || !rsi.TryGetState(state, out _))
            return;

        // Build layer
        var layer = new PrototypeLayerData();
        layer.RsiPath = rsi.Path.ToString();
        layer.State = state;

        // Set layer and play sound
        _sprite.LayerMapReserve(selected.Owner, "hallucination");
        _sprite.LayerSetData(selected.Owner, "hallucination", layer);
        if (_layers.TryAdd(GetNetEntity(selected), _timing.CurTime + TimeSpan.FromSeconds(5)))
            _audio.PlayEntity(args.Appearance.Sound, _player.LocalSession, selected);

        QueueDel(item);
    }

    private void OnGetStatusIcons(Entity<SchizophreniaComponent> ent, ref GetStatusIconsEvent args)
    {
        if (TryComp<HallucinationComponent>(_player.LocalEntity, out var hallucination) && hallucination.Idx == ent.Comp.Idx)
            args.StatusIcons.Add(_prototypeManager.Index<FactionIconPrototype>("ShizophrenicIcon"));
    }

    private void OnMusicInit(Entity<HallucinationsMusicComponent> ent, ref MapInitEvent args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        foreach (var item in ent.Comp.Music)
        {
            if (ent.Comp.ActiveMusic.ContainsKey(item.Key) || ent.Comp.NextMusic.ContainsKey(item.Key))
                continue;

            ent.Comp.NextMusic[item.Key] = _timing.CurTime + TimeSpan.FromSeconds(10f);
        }
    }

    private void OnMusicShutdown(Entity<HallucinationsMusicComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        foreach (var item in ent.Comp.ActiveMusic.ToList())
        {
            _contentAudio.FadeOut(item.Value, duration: 5f);
            ent.Comp.ActiveMusic.Remove(item.Key);
            ent.Comp.NextMusic.Remove(item.Key);
        }
    }

    private void OnMusicAttach(Entity<HallucinationsMusicComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        foreach (var item in ent.Comp.Music)
        {
            if (ent.Comp.ActiveMusic.ContainsKey(item.Key) || ent.Comp.NextMusic.ContainsKey(item.Key))
                continue;

            ent.Comp.NextMusic[item.Key] = _timing.CurTime + TimeSpan.FromSeconds(item.Value.Delay.HasValue ? item.Value.Delay.Value.Next(_random) : 10f);
        }
    }

    private void OnMusicDetach(Entity<HallucinationsMusicComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        foreach (var item in ent.Comp.ActiveMusic.ToList())
        {
            _contentAudio.FadeOut(item.Value, duration: 1f);
            ent.Comp.ActiveMusic.Remove(item.Key);
            ent.Comp.NextMusic.Remove(item.Key);
        }
    }

    private void OnMusicHandleState(Entity<HallucinationsMusicComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        foreach (var item in ent.Comp.Music)
        {
            if (ent.Comp.ActiveMusic.ContainsKey(item.Key) || ent.Comp.NextMusic.ContainsKey(item.Key))
                continue;

            ent.Comp.NextMusic[item.Key] = _timing.CurTime + TimeSpan.FromSeconds(10f);
        }

        foreach (var item in ent.Comp.ActiveMusic.ToList())
        {
            if (!ent.Comp.Music.ContainsKey(item.Key))
            {
                _contentAudio.FadeOut(item.Value, duration: 8f);
                ent.Comp.ActiveMusic.Remove(item.Key);
                ent.Comp.NextMusic.Remove(item.Key);
            }
        }
    }

    public bool CanSee(EntityUid target)
    {
        if (!HasComp<HallucinationsRemoveMobsComponent>(_player.LocalEntity))
            return true;

        if (target == _player.LocalEntity)
            return true;

        if (HasComp<HallucinationComponent>(target))
            return true;

        if (Transform(target).ParentUid == _player.LocalEntity)
            return true;

        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!TryComp<HallucinationsMusicComponent>(_player.LocalEntity, out var comp))
            return;

        foreach (var item in comp.NextMusic.ToDictionary())
        {
            if (item.Value > _timing.CurTime)
                continue;

            if (!comp.Music.TryGetValue(item.Key, out var music))
                continue;

            if (music.Delay.HasValue)
                comp.NextMusic[item.Key] = _timing.CurTime + TimeSpan.FromSeconds(music.Delay.Value.Next(_random));
            else
                comp.NextMusic.Remove(item.Key);

            var hasMusic = comp.ActiveMusic.TryGetValue(item.Key, out var exsisting) && exsisting.IsValid();

            var mus = _audio.PlayGlobal(music.Sound, _player.LocalEntity.Value, AudioParams.Default.WithLoop(music.Delay == null));
            if (!mus.HasValue)
                continue;

            comp.ActiveMusic[item.Key] = mus.Value.Entity;

            if (!hasMusic)
                _contentAudio.FadeIn(mus.Value.Entity, duration: 5f);
        }
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        FrameUpdateExtraLayers();
        FrameUpdateMobs();
    }

    private void FrameUpdateExtraLayers()
    {
        foreach (var item in _layers.ToDictionary())
        {
            if (item.Value > _timing.CurTime)
                continue;

            if (!TryGetEntity(item.Key, out var ent))
                continue;

            _sprite.RemoveLayer(ent.Value, "hallucination", false);
            _layers.Remove(item.Key);
        }
    }

    private void FrameUpdateMobs()
    {
        if (HasComp<HallucinationsRemoveMobsComponent>(_player.LocalEntity))
        {
            var ents = EntityManager.AllEntities<MobStateComponent>().Where(x => !HasComp<HallucinationComponent>(x)).ToList();
            foreach (var item in ents)
            {
                if (item.Owner == _player.LocalEntity || Transform(item.Owner).ParentUid == _player.LocalEntity)
                    continue;

                _transform.SetCoordinates(item.Owner, new EntityCoordinates(EntityUid.Invalid, Vector2.Zero));
            }
        }
    }
}
