using System.Linq;
using Content.Server.Administration;
using Content.Server.ADT.Surgery;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.ADT.Body.OrganOrigin;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.Administration;
using Content.Shared.Body;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Administration.Commands;

public abstract class SurgeryCommandBase : LocalizedEntityCommands
{
    [Dependency] protected IPrototypeManager Proto = default!;

    private static readonly Dictionary<string, string> LimbParents = new()
    {
        ["HandLeft"] = "ArmLeft",
        ["HandRight"] = "ArmRight",
        ["FootLeft"] = "LegLeft",
        ["FootRight"] = "LegRight",
    };

    protected bool TryParseEntity(IConsoleShell shell, string arg, out EntityUid uid)
    {
        if (TryParseEntitySilent(arg, out uid))
            return true;

        shell.WriteLine(Loc.GetString(int.TryParse(arg, out _) ? "shell-invalid-entity-id" : "shell-entity-uid-must-be-number"));
        return false;
    }

    protected bool TryParseEntitySilent(string arg, out EntityUid uid)
    {
        uid = default;

        if (!int.TryParse(arg, out var id)
            || !EntityManager.TryGetEntity(new NetEntity(id), out var found)
            || !EntityManager.EntityExists(found))
            return false;

        uid = found.Value;
        return true;
    }

    protected CompletionResult BodyCompletion(string text)
    {
        return CompletionResult.FromHintOptions(CompletionHelper.Components<BodyComponent>(text, EntityManager), Loc.GetString("cmd-organs-hint-entity"));
    }

    protected IEnumerable<string> PresentCategories<T>(string entityArg) where T : IComponent
    {
        if (!TryParseEntitySilent(entityArg, out var uid))
            return Proto.EnumeratePrototypes<OrganCategoryPrototype>().Select(x => x.ID);

        return EntityManager.System<BodySystem>()
            .EnumerateOrgans<T>(uid)
            .Select(o => o.Comp1.Category?.ToString())
            .Where(c => c != null)
            .Select(c => c!)
            .Distinct();
    }

    protected IEnumerable<string> OrganPrototypes()
    {
        return Proto.EnumeratePrototypes<EntityPrototype>()
            .Where(p => !p.Abstract && p.HasComp<OrganComponent>(EntityManager.ComponentFactory))
            .Select(p => p.ID)
            .OrderBy(id => id);
    }

    protected bool TryFindOrgan(EntityUid body, string category, out EntityUid organ)
    {
        foreach (var candidate in EntityManager.System<BodySystem>().EnumerateOrgans<OrganComponent>(body))
        {
            if (!string.Equals(candidate.Comp1.Category?.ToString(), category, StringComparison.OrdinalIgnoreCase))
                continue;

            organ = candidate.Owner;
            return true;
        }

        organ = default;
        return false;
    }

    protected bool InsertOrgan(EntityUid body, EntityUid organ, string? parentCategory)
    {
        var bodySystem = EntityManager.System<BodySystem>();

        if (!EntityManager.HasComponent<DetachableOrganComponent>(organ))
            return bodySystem.TryInsertOrgan(body, organ);

        var category = EntityManager.GetComponentOrNull<OrganComponent>(organ)?.Category?.ToString() ?? string.Empty;
        parentCategory ??= LimbParents.GetValueOrDefault(category, "Torso");

        if (!TryFindOrgan(body, parentCategory, out var parent) || !bodySystem.TryInsertOrgan(body, organ, parent))
            return false;

        EntityManager.System<OrganOriginSystem>().OnTransplanted(organ, body);
        return true;
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class ListOrgansCommand : SurgeryCommandBase
{
    [Dependency] private BodySystem _body = default!;

    public override string Command => "organs";

    public override string Description => Loc.GetString("cmd-organs-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!TryParseEntity(shell, args[0], out var uid))
            return;

        var count = 0;
        foreach (var organ in _body.EnumerateOrgans<OrganComponent>(uid))
        {
            count++;
            var category = organ.Comp1.Category?.ToString() ?? "-";

            if (EntityManager.TryGetComponent<OrganHealthComponent>(organ.Owner, out var health))
            {
                shell.WriteLine($"{category}: {EntityManager.ToPrettyString(organ.Owner)} {health.Kind} {health.Damage:0.#}/{health.MaxDamage:0.#} {health.Stage}{(health.Operated ? " operated" : "")}");
                continue;
            }

            shell.WriteLine($"{category}: {EntityManager.ToPrettyString(organ.Owner)}");
        }

        if (count == 0)
            shell.WriteLine(Loc.GetString("cmd-organs-none"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length == 1 ? BodyCompletion(args[0]) : CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class SetOrganDamageCommand : SurgeryCommandBase
{
    [Dependency] private BodySystem _body = default!;

    public override string Command => "organdamage";

    public override string Description => Loc.GetString("cmd-organdamage-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!TryParseEntity(shell, args[0], out var uid))
            return;

        if (!float.TryParse(args[2], out var damage))
        {
            shell.WriteLine(Loc.GetString("shell-argument-must-be-number"));
            return;
        }

        var all = args[1].Equals("all", StringComparison.OrdinalIgnoreCase);
        var health = EntityManager.System<OrganHealthSystem>();
        var changed = 0;

        foreach (var organ in _body.EnumerateOrgans<OrganHealthComponent>(uid))
        {
            if (!all && !string.Equals(organ.Comp1.Category?.ToString(), args[1], StringComparison.OrdinalIgnoreCase))
                continue;

            if (health.SetDamage((organ.Owner, organ.Comp2), damage))
                changed++;
        }

        shell.WriteLine(Loc.GetString("cmd-organdamage-result", ("count", changed)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => BodyCompletion(args[0]),
            2 => CompletionResult.FromHintOptions(PresentCategories<OrganHealthComponent>(args[0]).Prepend("all"), Loc.GetString("cmd-organdamage-hint-category")),
            3 => CompletionResult.FromHintOptions(new[] { "0", "10", "20", "40" }, Loc.GetString("cmd-organdamage-hint-damage")),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class RemoveOrganCommand : SurgeryCommandBase
{
    [Dependency] private BodySystem _body = default!;

    public override string Command => "organremove";

    public override string Description => Loc.GetString("cmd-organremove-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!TryParseEntity(shell, args[0], out var uid))
            return;

        var delete = false;
        if (args.Length == 3 && !bool.TryParse(args[2], out delete))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-bool"));
            return;
        }

        if (!TryFindOrgan(uid, args[1], out var organ))
        {
            shell.WriteLine(Loc.GetString("cmd-organ-not-found"));
            return;
        }

        var detachable = EntityManager.HasComponent<DetachableOrganComponent>(organ);
        if (!_body.TryRemoveOrgan(organ))
        {
            shell.WriteLine(Loc.GetString("cmd-organremove-failed"));
            return;
        }

        if (delete)
        {
            var target = detachable ? EntityManager.GetComponent<TransformComponent>(organ).ParentUid : organ;
            EntityManager.QueueDeleteEntity(target.IsValid() && target != uid ? target : organ);
        }

        shell.WriteLine(Loc.GetString(delete ? "cmd-organremove-deleted" : "cmd-organremove-done", ("organ", EntityManager.ToPrettyString(organ))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => BodyCompletion(args[0]),
            2 => CompletionResult.FromHintOptions(PresentCategories<OrganComponent>(args[0]), Loc.GetString("cmd-organ-hint-category")),
            3 => CompletionResult.FromHintOptions(new[] { "false", "true" }, Loc.GetString("cmd-organremove-hint-delete")),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class InsertOrganCommand : SurgeryCommandBase
{
    public override string Command => "organinsert";

    public override string Description => Loc.GetString("cmd-organinsert-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!TryParseEntity(shell, args[0], out var uid))
            return;

        var spawned = false;
        if (!TryParseEntitySilent(args[1], out var organ))
        {
            if (!Proto.TryIndex<EntityPrototype>(args[1], out var proto) || !proto.HasComp<OrganComponent>(EntityManager.ComponentFactory))
            {
                shell.WriteLine(Loc.GetString("cmd-organinsert-unknown"));
                return;
            }

            organ = EntityManager.SpawnNextToOrDrop(proto.ID, uid);
            spawned = true;
        }

        if (!EntityManager.TryGetComponent<OrganComponent>(organ, out var organComp))
        {
            shell.WriteLine(Loc.GetString("cmd-organinsert-unknown"));
            return;
        }

        if (organComp.Category is { } category && TryFindOrgan(uid, category, out _))
        {
            if (spawned)
                EntityManager.QueueDeleteEntity(organ);

            shell.WriteLine(Loc.GetString("cmd-organinsert-occupied"));
            return;
        }

        if (!InsertOrgan(uid, organ, args.Length == 3 ? args[2] : null))
        {
            if (spawned)
                EntityManager.QueueDeleteEntity(organ);

            shell.WriteLine(Loc.GetString("cmd-organinsert-failed"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-organinsert-done", ("organ", EntityManager.ToPrettyString(organ))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => BodyCompletion(args[0]),
            2 => CompletionResult.FromHintOptions(OrganPrototypes(), Loc.GetString("cmd-organinsert-hint-organ")),
            3 => CompletionResult.FromHintOptions(PresentCategories<OrganComponent>(args[0]), Loc.GetString("cmd-organinsert-hint-parent")),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class ReplaceOrganCommand : SurgeryCommandBase
{
    [Dependency] private BodySystem _body = default!;

    public override string Command => "organreplace";

    public override string Description => Loc.GetString("cmd-organreplace-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!TryParseEntity(shell, args[0], out var uid))
            return;

        if (!Proto.TryIndex<EntityPrototype>(args[1], out var proto)
            || !proto.TryComp<OrganComponent>(out var organProto, EntityManager.ComponentFactory)
            || organProto.Category is not { } category)
        {
            shell.WriteLine(Loc.GetString("cmd-organinsert-unknown"));
            return;
        }

        if (TryFindOrgan(uid, category, out var old))
        {
            var detachable = EntityManager.HasComponent<DetachableOrganComponent>(old);
            if (!_body.TryRemoveOrgan(old))
            {
                shell.WriteLine(Loc.GetString("cmd-organremove-failed"));
                return;
            }

            var leftover = detachable ? EntityManager.GetComponent<TransformComponent>(old).ParentUid : old;
            EntityManager.QueueDeleteEntity(leftover.IsValid() && leftover != uid ? leftover : old);
        }

        var organ = EntityManager.SpawnNextToOrDrop(proto.ID, uid);
        if (!InsertOrgan(uid, organ, null))
        {
            EntityManager.QueueDeleteEntity(organ);
            shell.WriteLine(Loc.GetString("cmd-organinsert-failed"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-organinsert-done", ("organ", EntityManager.ToPrettyString(organ))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => BodyCompletion(args[0]),
            2 => CompletionResult.FromHintOptions(OrganPrototypes(), Loc.GetString("cmd-organinsert-hint-organ")),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class SurgerySkillCommand : SurgeryCommandBase
{
    public override string Command => "surgeryskill";

    public override string Description => Loc.GetString("cmd-surgeryskill-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!TryParseEntity(shell, args[0], out var uid))
            return;

        var give = !EntityManager.HasComponent<SurgerySkillComponent>(uid);
        if (args.Length == 2 && !bool.TryParse(args[1], out give))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-bool"));
            return;
        }

        if (give)
            EntityManager.EnsureComponent<SurgerySkillComponent>(uid);
        else
            EntityManager.RemoveComponent<SurgerySkillComponent>(uid);

        shell.WriteLine(Loc.GetString(give ? "cmd-surgeryskill-given" : "cmd-surgeryskill-removed", ("entity", EntityManager.ToPrettyString(uid))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => BodyCompletion(args[0]),
            2 => CompletionResult.FromHintOptions(new[] { "true", "false" }, Loc.GetString("cmd-surgeryskill-hint")),
            _ => CompletionResult.Empty,
        };
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class SurgeryStateCommand : SurgeryCommandBase
{
    public override string Command => "surgerystate";

    public override string Description => Loc.GetString("cmd-surgerystate-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!TryParseEntity(shell, args[0], out var uid))
            return;

        if (!EntityManager.System<SurgerySystem>().TryGetOperationState(uid, out var graph, out var node, out var edges))
        {
            shell.WriteLine(Loc.GetString("cmd-surgerystate-none"));
            return;
        }

        shell.WriteLine($"{graph} / {node}");
        foreach (var (id, target, available) in edges)
        {
            shell.WriteLine($"  {id} -> {target}{(available ? "" : " (unavailable)")}");
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        return CompletionResult.FromHintOptions(CompletionHelper.Components<OperatedComponent>(args[0], EntityManager), Loc.GetString("cmd-organs-hint-entity"));
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class SurgeryGraphCommand : SurgeryCommandBase
{
    public override string Command => "surgerygraph";

    public override string Description => Loc.GetString("cmd-surgerygraph-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!TryParseEntity(shell, args[0], out var uid))
            return;

        var graph = args[1].Equals("none", StringComparison.OrdinalIgnoreCase) ? null : args[1];
        var node = args.Length == 3 ? args[2] : null;

        if (!EntityManager.System<SurgerySystem>().TrySetGraph(uid, graph, node))
        {
            shell.WriteLine(Loc.GetString("cmd-surgerygraph-invalid"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-surgerygraph-done"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return BodyCompletion(args[0]);
            case 2:
                var graphs = Proto.EnumeratePrototypes<SurgeryGraphPrototype>().Select(x => x.ID).OrderBy(x => x).Prepend("none");
                return CompletionResult.FromHintOptions(graphs, Loc.GetString("cmd-surgerygraph-hint"));
            case 3:
                if (!Proto.TryIndex<SurgeryGraphPrototype>(args[1], out var proto))
                    return CompletionResult.Empty;

                return CompletionResult.FromHintOptions(proto.Nodes.Select(n => n.Name), Loc.GetString("cmd-surgerygraph-hint-node"));
            default:
                return CompletionResult.Empty;
        }
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class SurgeryDoCommand : SurgeryCommandBase
{
    public override string Command => "surgerydo";

    public override string Description => Loc.GetString("cmd-surgerydo-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!TryParseEntity(shell, args[0], out var uid))
            return;

        EntityUid? surgeon = shell.Player?.AttachedEntity;

        if (!EntityManager.System<SurgerySystem>().TryForceEdge(uid, args[1], surgeon))
        {
            shell.WriteLine(Loc.GetString("cmd-surgerydo-failed"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-surgerydo-done"));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                return CompletionResult.FromHintOptions(CompletionHelper.Components<OperatedComponent>(args[0], EntityManager), Loc.GetString("cmd-organs-hint-entity"));
            case 2:
                if (!TryParseEntitySilent(args[0], out var uid)
                    || !EntityManager.System<SurgerySystem>().TryGetOperationState(uid, out _, out _, out var edges))
                    return CompletionResult.Empty;

                return CompletionResult.FromHintOptions(edges.Where(e => e.Available).Select(e => e.Id), Loc.GetString("cmd-surgerydo-hint"));
            default:
                return CompletionResult.Empty;
        }
    }
}
