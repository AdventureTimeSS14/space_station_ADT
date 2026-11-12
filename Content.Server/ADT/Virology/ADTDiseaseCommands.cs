using System.Linq;
using Content.Server.Administration;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.Administration;
using Content.Shared.Mobs.Components;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Virology;

[AdminCommand(AdminFlags.Debug)]
public sealed class ADTInfectCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entMan = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public string Command => "adtinfect";
    public string Description => Loc.GetString("adt-cmd-infect-desc");
    public string Help => Loc.GetString("adt-cmd-infect-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Help);
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEnt) || !_entMan.TryGetEntity(netEnt, out var uid))
        {
            shell.WriteError(Loc.GetString("adt-cmd-virology-no-entity"));
            return;
        }

        var system = _entMan.System<ADTDiseaseSystem>();
        ADTDisease? disease;

        if (args[1] == "advance")
        {
            var symptoms = new List<ProtoId<ADTSymptomPrototype>>();
            foreach (var id in args.Skip(2))
            {
                if (!_proto.HasIndex<ADTSymptomPrototype>(id))
                {
                    shell.WriteError(Loc.GetString("adt-cmd-virology-no-symptom", ("id", id)));
                    return;
                }

                symptoms.Add(id);
            }

            disease = system.CreateAdvance(symptoms.Count > 0 ? symptoms : null);
        }
        else
        {
            if (!_proto.TryIndex<ADTDiseasePrototype>(args[1], out var proto))
            {
                shell.WriteError(Loc.GetString("adt-cmd-virology-no-disease", ("id", args[1])));
                return;
            }

            disease = proto.Symptoms.Count > 0
                ? system.CreatePresetAdvance(proto)
                : ADTDisease.FromPrototype(proto);
        }

        var result = system.Contract(uid.Value, disease);
        if (result == null)
        {
            shell.WriteError(Loc.GetString("adt-cmd-infect-failed"));
            return;
        }

        shell.WriteLine(Loc.GetString("adt-cmd-infect-done",
            ("name", system.GetDiseaseName(result)),
            ("id", result.GetId())));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<MobStateComponent>(args[0], _entMan),
                Loc.GetString("adt-cmd-virology-hint-uid"));
        }

        if (args.Length == 2)
        {
            var options = _proto.EnumeratePrototypes<ADTDiseasePrototype>()
                .Where(p => !p.Abstract)
                .Select(p => p.ID)
                .OrderBy(id => id)
                .Prepend("advance");

            return CompletionResult.FromHintOptions(options, Loc.GetString("adt-cmd-virology-hint-disease"));
        }

        if (args.Length > 2 && args[1] == "advance")
        {
            var used = args.Skip(2).Take(args.Length - 3).ToHashSet();
            var options = _proto.EnumeratePrototypes<ADTSymptomPrototype>()
                .Select(p => p.ID)
                .Where(id => !used.Contains(id))
                .OrderBy(id => id);

            return CompletionResult.FromHintOptions(options, Loc.GetString("adt-cmd-virology-hint-symptom"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class ADTCureCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entMan = default!;

    public string Command => "adtcure";
    public string Description => Loc.GetString("adt-cmd-cure-desc");
    public string Help => Loc.GetString("adt-cmd-cure-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || !NetEntity.TryParse(args[0], out var netEnt) || !_entMan.TryGetEntity(netEnt, out var uid))
        {
            shell.WriteError(Help);
            return;
        }

        var immunity = args.Length < 2 || !bool.TryParse(args[1], out var parsed) || parsed;
        _entMan.System<ADTDiseaseSystem>().CureAll(uid.Value, immunity);
        shell.WriteLine(Loc.GetString("adt-cmd-virology-done"));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<ADTDiseaseCarrierComponent>(args[0], _entMan),
                Loc.GetString("adt-cmd-virology-hint-uid"));
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                new[] { "true", "false" },
                Loc.GetString("adt-cmd-cure-hint-immunity"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class ADTDiseasesCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entMan = default!;

    public string Command => "adtdiseases";
    public string Description => Loc.GetString("adt-cmd-diseases-desc");
    public string Help => Loc.GetString("adt-cmd-diseases-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1 || !NetEntity.TryParse(args[0], out var netEnt) || !_entMan.TryGetEntity(netEnt, out var uid))
        {
            shell.WriteError(Help);
            return;
        }

        var system = _entMan.System<ADTDiseaseSystem>();
        var diseases = system.GetDiseases(uid.Value);
        for (var i = 0; i < diseases.Count; i++)
        {
            var d = diseases[i];
            shell.WriteLine($"[{i}] {system.GetDiseaseName(d)} id={d.GetId()} stage={d.Stage}/{d.MaxStages} " +
                            $"stageProb={d.StageProb} cureProb={d.CureProb} spread={d.Spread} vis={d.Visibility} " +
                            $"sev={d.Severity} cures={string.Join(",", d.Cures)} carrier={d.Carrier} discovered={d.Discovered}");
        }

        if (_entMan.TryGetComponent<ADTDiseaseCarrierComponent>(uid, out var carrier))
            shell.WriteLine(Loc.GetString("adt-cmd-diseases-resistances", ("list", string.Join(", ", carrier.Resistances))));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<MobStateComponent>(args[0], _entMan),
                Loc.GetString("adt-cmd-virology-hint-uid"));
        }

        return CompletionResult.Empty;
    }
}

[AdminCommand(AdminFlags.Debug)]
public sealed class ADTDiseaseStageCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entMan = default!;

    public string Command => "adtdiseasestage";
    public string Description => Loc.GetString("adt-cmd-stage-desc");
    public string Help => Loc.GetString("adt-cmd-stage-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3
            || !NetEntity.TryParse(args[0], out var netEnt)
            || !_entMan.TryGetEntity(netEnt, out var uid)
            || !int.TryParse(args[1], out var index)
            || !int.TryParse(args[2], out var stage))
        {
            shell.WriteError(Help);
            return;
        }

        var system = _entMan.System<ADTDiseaseSystem>();
        var diseases = system.GetDiseases(uid.Value);
        if (index < 0 || index >= diseases.Count)
        {
            shell.WriteError(Loc.GetString("adt-cmd-stage-bad-index"));
            return;
        }

        system.ForceStage(uid.Value, diseases[index], stage);
        shell.WriteLine(Loc.GetString("adt-cmd-virology-done"));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.Components<ADTDiseaseCarrierComponent>(args[0], _entMan),
                Loc.GetString("adt-cmd-virology-hint-uid"));
        }

        if (args.Length == 2)
        {
            var options = new List<CompletionOption>();
            if (NetEntity.TryParse(args[0], out var netEnt) && _entMan.TryGetEntity(netEnt, out var uid))
            {
                var system = _entMan.System<ADTDiseaseSystem>();
                var diseases = system.GetDiseases(uid.Value);
                for (var i = 0; i < diseases.Count; i++)
                {
                    options.Add(new CompletionOption(i.ToString(), system.GetDiseaseName(diseases[i])));
                }
            }

            return CompletionResult.FromHintOptions(options, Loc.GetString("adt-cmd-stage-hint-index"));
        }

        if (args.Length == 3)
        {
            var max = 5;
            if (NetEntity.TryParse(args[0], out var netEnt)
                && _entMan.TryGetEntity(netEnt, out var uid)
                && int.TryParse(args[1], out var index))
            {
                var diseases = _entMan.System<ADTDiseaseSystem>().GetDiseases(uid.Value);
                if (index >= 0 && index < diseases.Count)
                    max = diseases[index].MaxStages;
            }

            return CompletionResult.FromHintOptions(
                Enumerable.Range(1, max).Select(i => i.ToString()),
                Loc.GetString("adt-cmd-stage-hint-stage"));
        }

        return CompletionResult.Empty;
    }
}
