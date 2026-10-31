using System.Linq;
using Content.Server.Administration;
using Content.Server.ADT.Surgery;
using Content.Shared.ADT.Body.OrganHealth;
using Content.Shared.ADT.Surgery.Components;
using Content.Shared.ADT.Surgery.Prototypes;
using Content.Shared.Administration;
using Content.Shared.Body;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Administration.Commands;

public abstract class SurgeryCommandBase : LocalizedEntityCommands
{
    protected bool TryParseEntity(IConsoleShell shell, string arg, out EntityUid uid)
    {
        uid = default;

        if (!int.TryParse(arg, out var id))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
            return false;
        }

        if (!EntityManager.TryGetEntity(new NetEntity(id), out var found) || !EntityManager.EntityExists(found))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
            return false;
        }

        uid = found.Value;
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
}

[AdminCommand(AdminFlags.Debug)]
public sealed class SetOrganDamageCommand : SurgeryCommandBase
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private IPrototypeManager _proto = default!;

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
        if (args.Length == 1)
            return CompletionResult.FromHint(Loc.GetString("cmd-organs-hint-entity"));

        if (args.Length == 2)
        {
            var options = new List<string> { "all" };
            options.AddRange(_proto.EnumeratePrototypes<OrganCategoryPrototype>().Select(x => x.ID));
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-organdamage-hint-category"));
        }

        return CompletionResult.Empty;
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
}

[AdminCommand(AdminFlags.Debug)]
public sealed class SurgeryGraphCommand : SurgeryCommandBase
{
    [Dependency] private IPrototypeManager _proto = default!;

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
        if (args.Length != 2)
            return CompletionResult.Empty;

        var options = new List<string> { "none" };
        options.AddRange(_proto.EnumeratePrototypes<SurgeryGraphPrototype>().Select(x => x.ID));
        return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-surgerygraph-hint"));
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
}
