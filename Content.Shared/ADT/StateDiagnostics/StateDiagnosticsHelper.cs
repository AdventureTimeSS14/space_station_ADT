using System.Linq;
using System.Text;
using Content.Shared.ADT.Components.PickupHumans;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.StateDiagnostics;

public static class StateDiagnosticsHelper
{
    public const int MaxReportLength = 32000;

    private const int MaxListed = 25;
    private const int MaxDepth = 64;

    public static string DescribeParentChain(IEntityManager entMan, EntityUid uid)
    {
        var sb = new StringBuilder();
        var current = uid;

        for (var depth = 0; depth < MaxDepth; depth++)
        {
            if (depth > 0)
                sb.Append(" -> ");

            if (!entMan.TryGetComponent(current, out TransformComponent? xform))
            {
                sb.Append($"{current} (no transform)");
                return sb.ToString();
            }

            sb.Append(entMan.ToPrettyString(current).ToString());

            if (!xform.ParentUid.IsValid())
                return sb.ToString();

            current = xform.ParentUid;
        }

        sb.Append(" -> ... (cycle or deeper than 64)");
        return sb.ToString();
    }

    public static void AppendEntityScan(IEntityManager entMan, IEnumerable<EntityUid> uids, StringBuilder sb, bool checkStateApplied)
    {
        var scanned = 0;
        var problems = new List<string>();
        var attached = new List<string>();
        var noState = new List<string>();
        var problemCount = 0;
        var attachedCount = 0;
        var noStateCount = 0;

        foreach (var uid in uids)
        {
            if (!entMan.TryGetComponent(uid, out TransformComponent? xform)
                || !entMan.TryGetComponent(uid, out MetaDataComponent? meta))
                continue;

            scanned++;

            if (GetTransformProblem(entMan, uid, xform) is { } problem)
            {
                problemCount++;
                if (problems.Count < MaxListed)
                    problems.Add($"{entMan.ToPrettyString(uid)}: {problem}");
            }

            if (IsMobAttachedToEntity(entMan, uid, xform, meta))
            {
                attachedCount++;
                if (attached.Count < MaxListed)
                    attached.Add(DescribeAttached(entMan, uid, xform));
            }

            if (checkStateApplied
                && !entMan.IsClientSide(uid, meta)
                && meta.LastStateApplied == GameTick.Zero)
            {
                noStateCount++;
                if (noState.Count < MaxListed)
                    noState.Add($"{entMan.ToPrettyString(uid)} [{DescribeComponents(entMan, uid)}]");
            }
        }

        sb.AppendLine($"Scanned entities: {scanned}");
        AppendList(sb, "Transform problems", problems, problemCount);
        AppendList(sb, "Mobs parented to an entity that is not a grid, map or container", attached, attachedCount);

        if (checkStateApplied)
            AppendList(sb, "Networked entities without an applied state", noState, noStateCount);
    }

    private static string? GetTransformProblem(IEntityManager entMan, EntityUid uid, TransformComponent xform)
    {
        var current = xform;
        var currentUid = uid;

        for (var depth = 0; depth < MaxDepth; depth++)
        {
            var parent = current.ParentUid;

            if (!parent.IsValid())
            {
                if (current.MapID != MapId.Nullspace && !entMan.HasComponent<MapComponent>(currentUid))
                    return $"root {entMan.ToPrettyString(currentUid)} is not a map but is on map {current.MapID}";

                return null;
            }

            if (!entMan.TryGetComponent(parent, out MetaDataComponent? parentMeta)
                || !entMan.TryGetComponent(parent, out TransformComponent? parentXform))
                return $"parent {parent} does not exist";

            if (parentMeta.EntityLifeStage >= EntityLifeStage.Terminating)
                return $"parent {entMan.ToPrettyString(parent)} is {parentMeta.EntityLifeStage}";

            current = parentXform;
            currentUid = parent;
        }

        return "parent chain is a cycle or deeper than 64";
    }

    private static bool IsMobAttachedToEntity(IEntityManager entMan, EntityUid uid, TransformComponent xform, MetaDataComponent meta)
    {
        return entMan.HasComponent<MobStateComponent>(uid)
               && xform.ParentUid.IsValid()
               && xform.ParentUid != xform.GridUid
               && xform.ParentUid != xform.MapUid
               && (meta.Flags & MetaDataFlags.InContainer) == 0;
    }

    private static string DescribeAttached(IEntityManager entMan, EntityUid uid, TransformComponent xform)
    {
        var parts = new List<string>
        {
            $"{entMan.ToPrettyString(uid)} parent={entMan.ToPrettyString(xform.ParentUid)}",
        };

        if (entMan.TryGetComponent(uid, out TakenHumansComponent? taken))
            parts.Add($"carriedBy={entMan.ToPrettyString(taken.Carrier)}");

        if (entMan.HasComponent<SlimeLatchedComponent>(uid))
            parts.Add("slimeLatched");

        if (entMan.TryGetComponent(uid, out PhysicsComponent? physics))
            parts.Add($"body={physics.BodyType} canCollide={physics.CanCollide}");

        return string.Join(" ", parts);
    }

    private static string DescribeComponents(IEntityManager entMan, EntityUid uid)
    {
        return string.Join(", ", entMan.GetComponents(uid).Select(c => entMan.ComponentFactory.GetComponentName(c.GetType())));
    }

    private static void AppendList(StringBuilder sb, string title, List<string> lines, int total)
    {
        sb.AppendLine($"{title}: {total}");

        foreach (var line in lines)
        {
            sb.AppendLine($"  {line}");
        }

        if (total > lines.Count)
            sb.AppendLine($"  ... and {total - lines.Count} more");
    }
}
