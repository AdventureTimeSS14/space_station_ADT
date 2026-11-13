#nullable enable
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests.Minds;

[TestFixture]
public sealed partial class MindTests
{
    [Test]
    public async Task DeleteAllThenGhost()
    {
        var pair = Pair;

        // Client is connected with a valid entity & mind
        Assert.That(pair.Client.EntMan.EntityExists(pair.Client.AttachedEntity));
        Assert.That(pair.Server.EntMan.EntityExists(pair.PlayerData?.Mind));

        // Delete **everything**
        var conHost = pair.Server.ResolveDependency<IConsoleHost>();
        await pair.Server.WaitPost(() => conHost.ExecuteCommand("entities delete"));
        await pair.RunTicksSync(5);

        // ADT-Tweak start
        var leftovers = new System.Collections.Generic.List<string>();
        foreach (var ent in pair.Server.EntMan.GetEntities())
        {
            var leftoverXform = pair.Server.EntMan.GetComponentOrNull<TransformComponent>(ent);
            leftovers.Add($"{pair.Server.EntMan.ToPrettyString(ent)} parent={leftoverXform?.ParentUid} map={leftoverXform?.MapID}");
        }

        Assert.That(pair.Server.EntMan.EntityCount, Is.EqualTo(0), $"Server entities left: {string.Join("; ", leftovers)}");
        // ADT-Tweak end

        foreach (var ent in pair.Client.EntMan.GetEntities())
        {
            Console.WriteLine(pair.Client.EntMan.ToPrettyString(ent));
        }

        Assert.That(pair.Client.EntMan.EntityCount, Is.EqualTo(0));

        // Create a new map.
        MapId mapId = default;
        await pair.Server.WaitPost(() => pair.Server.System<SharedMapSystem>().CreateMap(out mapId));
        await pair.RunTicksSync(5);

        // Client is not attached to anything
        Assert.That(pair.Client.AttachedEntity, Is.Null);
        Assert.That(pair.PlayerData?.Mind, Is.Null);

        // Attempt to ghost
        var cConHost = pair.Client.ResolveDependency<IConsoleHost>();
        await pair.Client.WaitPost(() => cConHost.ExecuteCommand("ghost"));
        await pair.RunTicksSync(10);

        // Client should be attached to a ghost placed on the new map.
        Assert.That(pair.Client.EntMan.EntityExists(pair.Client.AttachedEntity));
        Assert.That(pair.Server.EntMan.EntityExists(pair.PlayerData?.Mind));
        var xform = pair.Client.Transform(pair.Client.AttachedEntity!.Value);
        Assert.That(xform.MapID, Is.EqualTo(mapId));
    }
}
