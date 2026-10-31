using Content.IntegrationTests.Fixtures;
using Content.Shared.ADT.TapeRecorder;
using Content.Shared.Interaction;
using Content.Shared.Actions.Components;
using Robust.Shared.Audio.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.ADT;

public sealed class CassetteTests : GameTest
{
    [Test]
    public async Task PlaybackAndRemoval()
    {
        await Client.WaitPost(() =>
        {
            foreach (var (id, state) in new[]
            {
                ("ADTActionCassettePlayPause", "walkman_playpause"),
                ("ADTActionCassetteNext", "walkman_next"),
                ("ADTActionCassetteRestart", "walkman_restart"),
            })
            {
                var action = Client.EntMan.SpawnEntity(id, Robust.Shared.Map.MapCoordinates.Nullspace);
                var sprite = Client.EntMan.GetComponent<Robust.Client.GameObjects.SpriteComponent>(action);
                Assert.That(sprite.Icon, Is.Not.Null);
                Assert.That(sprite.LayerGetState(0).ToString(), Is.EqualTo(state));
                Client.EntMan.DeleteEntity(action);
            }
        });
        var map = await Pair.CreateTestMap();
        await Server.WaitPost(() =>
        {
            var em = Server.EntMan;
            var originalActor = ServerSession!.AttachedEntity;
            var actor = em.SpawnEntity("MobHuman", map.GridCoords);
            Server.PlayerMan.SetAttachedEntity(ServerSession, actor);
            var player = em.SpawnEntity("ADTCassettePlayer", map.GridCoords);
            var tape = em.SpawnEntity("ADTCassetteTapeClassic", map.GridCoords);
            var comp = em.GetComponent<CassettePlayerComponent>(player);
            foreach (var id in new[] { "ADTActionCassettePlayPause", "ADTActionCassetteNext", "ADTActionCassetteRestart" })
            {
                var action = em.SpawnEntity(id, map.GridCoords);
                var actionComp = em.GetComponent<ActionComponent>(action);
                Assert.That(actionComp.ItemIconStyle, Is.EqualTo(ItemActionIconStyle.NoItem));
                em.DeleteEntity(action);
            }
            var nuclear = em.SpawnEntity("ADTCassetteTapeNuclear", map.GridCoords);
            Assert.That(em.GetComponent<CassetteTapeComponent>(nuclear).Songs, Has.Count.EqualTo(6));
            em.DeleteEntity(nuclear);
            var custom = em.SpawnEntity("ADTCassetteTapeCustom", map.GridCoords);
            Assert.That(em.GetComponent<CassetteTapeComponent>(custom).Custom, Is.True);
            Assert.That(em.GetComponent<CassetteTapeComponent>(custom).Songs, Is.Empty);
            em.DeleteEntity(custom);


            // Empty players must not enter the playing state.
            var empty = new CassettePlayPauseActionEvent { Performer = actor };
            em.EventBus.RaiseLocalEvent(player, empty);
            Assert.That(empty.Handled, Is.False);

            var insert = new InteractUsingEvent(actor, tape, player, map.GridCoords);
            em.EventBus.RaiseLocalEvent(player, insert);
            Assert.That(insert.Handled, Is.True);

            em.EventBus.RaiseLocalEvent(player, new CassettePlayPauseActionEvent { Performer = actor });
            Assert.That(comp.State, Is.EqualTo(AudioState.Playing));
            Assert.That(comp.AudioStream, Is.Not.Null);
            em.EventBus.RaiseLocalEvent(player, new CassettePlayPauseActionEvent { Performer = actor });
            Assert.That(comp.State, Is.EqualTo(AudioState.Paused));
            em.EventBus.RaiseLocalEvent(player, new CassettePlayPauseActionEvent { Performer = actor });
            Assert.That(comp.State, Is.EqualTo(AudioState.Playing));

            em.EventBus.RaiseLocalEvent(player, new CassetteNextActionEvent { Performer = actor });
            Assert.That(comp.Tape, Is.EqualTo(1));
            em.EventBus.RaiseLocalEvent(player, new CassetteRestartActionEvent { Performer = actor });
            Assert.That(comp.Tape, Is.EqualTo(1));
            for (var i = 0; i < 6; i++)
                em.EventBus.RaiseLocalEvent(player, new CassetteNextActionEvent { Performer = actor });
            Assert.That(comp.Tape, Is.Zero);

            var containers = Server.System<SharedContainerSystem>();
            Assert.That(containers.TryGetContainer(player, comp.ContainerId, out var container), Is.True);
            var verbs = new Content.Shared.Verbs.GetVerbsEvent<Content.Shared.Verbs.AlternativeVerb>(
                actor, player, null, em.GetComponent<Content.Shared.Hands.Components.HandsComponent>(actor),
                true, true, true, new System.Collections.Generic.List<Content.Shared.Verbs.VerbCategory>());
            em.EventBus.RaiseLocalEvent(player, verbs);
            var eject = System.Linq.Enumerable.Single(verbs.Verbs, verb => verb.Text ==
                Robust.Shared.Localization.Loc.GetString("adt-cassette-player-eject"));
            eject.Act!.Invoke();
            Assert.That(container!.ContainedEntities, Is.Empty);
            Assert.That(System.Linq.Enumerable.Any(em.EntityQuery<AudioComponent>(), audio =>
                audio.FileName == "/Audio/ADT/Items/Taperecorder/cassette_insert_eject.ogg"), Is.True);
            Assert.That(comp.State, Is.EqualTo(AudioState.Stopped));
            Assert.That(comp.AudioStream, Is.Null);

            em.DeleteEntity(tape);
            em.DeleteEntity(player);
            Server.PlayerMan.SetAttachedEntity(ServerSession, originalActor);
            em.DeleteEntity(actor);
        });
        await Server.WaitPost(() => Server.System<SharedMapSystem>().DeleteMap(map.MapId));
    }

#pragma warning disable RA0002 // Regression test constructs authoritative and client-local state.
    [TestCase(false)]
    [TestCase(true)]
    [TestCase(null, TestName = "CustomTrackSurvivesPastPrediction")]
    public async Task CustomTrackStopsOnEjection(bool? containerRemoval)
    {
        await Client.WaitPost(() =>
        {
            var em = Client.EntMan;
            var player = em.SpawnEntity("ADTCassettePlayer", Robust.Shared.Map.MapCoordinates.Nullspace);
            var tape = em.SpawnEntity("ADTCassetteTapeCustom", Robust.Shared.Map.MapCoordinates.Nullspace);
            var comp = em.GetComponent<CassettePlayerComponent>(player);
            var containers = Client.System<SharedContainerSystem>();
            var slot = containers.EnsureContainer<ContainerSlot>(player, comp.ContainerId);
            Assert.That(containers.Insert(tape, slot), Is.True);
            // The authoritative stop case represents a removed tape, not an older
            // server snapshot while a predicted Play still has its tape inserted.
            if (containerRemoval == false)
                Assert.That(containers.Remove(tape, slot, reparent: false), Is.True);
            var audio = Client.System<Robust.Client.Audio.AudioSystem>().PlayGlobal(
                new Robust.Shared.Audio.SoundPathSpecifier("/Audio/Jukebox/flip-flap.ogg"), player);
            Assert.That(audio, Is.Not.Null);
            comp.CustomAudioStream = audio!.Value.Entity;
            comp.State = AudioState.Playing;
            var wasPlaying = audio.Value.Component.Playing;
            var timing = Client.ResolveDependency<Robust.Client.Timing.IClientGameTiming>();
            try
            {
                // Container/state replication does not run in first-time prediction.
                if (containerRemoval is null)
                {
                    timing.StartPastPrediction();
                    try
                    {
                        em.EventBus.RaiseLocalEvent(player, new CassetteRestartActionEvent { Performer = player });
                    }
                    finally
                    {
                        timing.EndPastPrediction();
                    }
                    Assert.That(comp.CustomAudioStream, Is.EqualTo(audio.Value.Entity));
                    Assert.That(audio.Value.Component.State, Is.EqualTo(AudioState.Playing));
                    Assert.That(audio.Value.Component.Playing, Is.EqualTo(wasPlaying));
                    return;
                }

                timing.StartStateApplication();
                try
                {
                    if (containerRemoval == true)
                        Assert.That(containers.Remove(tape, slot, reparent: false), Is.True);
                    else
                    {
                        comp.State = AudioState.Stopped;
                        var ev = new Robust.Shared.Analyzers.AfterAutoHandleStateEvent(default!);
                        em.EventBus.RaiseComponentEvent(player, comp, ref ev);
                    }
                }
                finally
                {
                    timing.EndStateApplication();
                }
                Assert.That(comp.CustomAudioStream, Is.Null);
                Assert.That(audio.Value.Component.State, Is.EqualTo(AudioState.Stopped));
                Assert.That(audio.Value.Component.Playing, Is.False);
                Assert.That(comp.State, Is.EqualTo(AudioState.Stopped));
            }
            finally
            {
                em.DeleteEntity(audio.Value.Entity);
                em.DeleteEntity(tape);
                em.DeleteEntity(player);
            }
        });
    }
#pragma warning restore RA0002

#pragma warning disable RA0002 // Exercise client-local audio and server state correction.
    [TestCase(false)]
    [TestCase(true)]
    public async Task CustomPlayPauseStartsWithoutNext(bool serverCorrection)
    {
        await Client.WaitPost(() =>
        {
            var em = Client.EntMan;
            var player = em.SpawnEntity("ADTCassettePlayer", Robust.Shared.Map.MapCoordinates.Nullspace);
            var tape = em.SpawnEntity("ADTCassetteTapeCustom", Robust.Shared.Map.MapCoordinates.Nullspace);
            var comp = em.GetComponent<CassettePlayerComponent>(player);
            var tapeComp = em.GetComponent<CassetteTapeComponent>(tape);
            var containers = Client.System<SharedContainerSystem>();
            var slot = containers.EnsureContainer<ContainerSlot>(player, comp.ContainerId);
            Assert.That(containers.Insert(tape, slot), Is.True);

            // Register a real decoded OGG as ChooseCustomTrack does, without opening a native dialog.
            var cache = Client.ResolveDependency<Robust.Client.ResourceManagement.IResourceCache>();
            var stream = cache.GetResource<Robust.Client.ResourceManagement.AudioResource>(
                new Robust.Shared.Utility.ResPath("/Audio/Jukebox/flip-flap.ogg")).AudioStream;
            tapeComp.CustomTrack = stream;
            var system = Client.System<Content.Client.ADT.TapeRecorder.CassetteSystem>();
            var names = (System.Collections.Generic.Dictionary<Robust.Client.Audio.AudioStream, string>)
                typeof(Content.Client.ADT.TapeRecorder.CassetteSystem).GetField("_names",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(system)!;
            names[stream] = "/Audio/Jukebox/flip-flap.ogg";
            var timing = Client.ResolveDependency<Robust.Client.Timing.IClientGameTiming>();
            try
            {
                em.EventBus.RaiseLocalEvent(player, new CassettePlayPauseActionEvent { Performer = player });
                Assert.That(comp.CustomAudioStream, Is.Not.Null, "First Play/Pause must start the custom OGG without Next.");
                var first = comp.CustomAudioStream!.Value;
                Assert.That(em.GetComponent<AudioComponent>(first).State, Is.EqualTo(AudioState.Playing));

                if (serverCorrection)
                {
                    timing.StartStateApplication();
                    try
                    {
                        comp.State = AudioState.Stopped;
                        var state = new Robust.Shared.Analyzers.AfterAutoHandleStateEvent(default!);
                        em.EventBus.RaiseComponentEvent(player, comp, ref state);
                    }
                    finally { timing.EndStateApplication(); }
                    Assert.That(comp.CustomAudioStream, Is.EqualTo(first),
                        "An older stopped snapshot with the cassette still inserted must not kill predicted Play.");

                    timing.StartPastPrediction();
                    try { em.EventBus.RaiseLocalEvent(player, new CassettePlayPauseActionEvent { Performer = player }); }
                    finally { timing.EndPastPrediction(); }
                    Assert.That(em.GetComponent<AudioComponent>(first).State, Is.EqualTo(AudioState.Playing));
                }

                em.EventBus.RaiseLocalEvent(player, new CassettePlayPauseActionEvent { Performer = player });
                Assert.That(comp.CustomAudioStream, Is.EqualTo(first));
                Assert.That(em.GetComponent<AudioComponent>(first).State, Is.EqualTo(AudioState.Paused));
                em.EventBus.RaiseLocalEvent(player, new CassettePlayPauseActionEvent { Performer = player });
                Assert.That(comp.CustomAudioStream, Is.EqualTo(first));
                Assert.That(em.GetComponent<AudioComponent>(first).State, Is.EqualTo(AudioState.Playing));

                // Recover a previously stuck player whose network state says Playing but whose local stream ended.
                em.DeleteEntity(first);
                comp.State = AudioState.Playing;
                em.EventBus.RaiseLocalEvent(player, new CassettePlayPauseActionEvent { Performer = player });
                Assert.That(comp.CustomAudioStream, Is.Not.Null);
                Assert.That(comp.CustomAudioStream, Is.Not.EqualTo(first));
                Assert.That(em.GetComponent<AudioComponent>(comp.CustomAudioStream!.Value).State, Is.EqualTo(AudioState.Playing));
            }
            finally
            {
                containers.Remove(tape, slot, reparent: false);
                em.DeleteEntity(tape);
                em.DeleteEntity(player);
            }
        });
    }
#pragma warning restore RA0002
}
