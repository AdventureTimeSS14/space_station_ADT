using System.Linq;
using Content.Shared.Administration;
using Content.Server.Administration;
using Robust.Shared.Console;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class EntCountCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private static Dictionary<string, int>? _snapshot;

    public string Command => "entcount";

    public string Description =>
        "Counts live entities by prototype. Raw top is mostly static map geometry; for leaks use 'snapshot' then 'diff'.";

    public string Help =>
        "entcount [top] [filter] - current top N (default 30) by live count\n" +
        "entcount snapshot - save current counts as a baseline\n" +
        "entcount diff [top] [filter] - top N prototypes by GROWTH since snapshot";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var sub = args.Length >= 1 ? args[0].ToLowerInvariant() : "";
        var counts = Count();

        if (sub == "snapshot")
        {
            _snapshot = counts;
            shell.WriteLine($"Snapshot saved: {counts.Values.Sum()} entities across {counts.Count} prototypes.");
            shell.WriteLine("Wait 15-20 min at stable online, then run: entcount diff");
            return;
        }

        if (sub == "diff")
        {
            if (_snapshot == null)
            {
                shell.WriteLine("No snapshot yet. Run 'entcount snapshot' first, wait, then 'entcount diff'.");
                return;
            }

            var (topD, filterD) = ParseTopFilter(args, 1);
            var rows = counts.Keys.Union(_snapshot.Keys)
                .Select(p => (proto: p,
                              delta: counts.GetValueOrDefault(p) - _snapshot.GetValueOrDefault(p),
                              now: counts.GetValueOrDefault(p)))
                .Where(x => x.delta != 0)
                .Where(x => filterD == null || x.proto.Contains(filterD, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.delta)
                .Take(topD)
                .ToList();

            shell.WriteLine($"Growth since snapshot (top {rows.Count}) - biggest climbers are the leak. Static map cancels out:");
            shell.WriteLine("   delta        now  prototype");
            foreach (var x in rows)
                shell.WriteLine($"{x.delta.ToString("+#;-#;0"),8} {x.now,8}  {x.proto}");
            return;
        }

        var (top, filter) = ParseTopFilter(args, 0);
        var list = counts
            .Where(kv => filter == null || kv.Key.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(kv => kv.Value)
            .Take(top)
            .ToList();

        shell.WriteLine($"Total entities: {counts.Values.Sum()} | distinct prototypes: {counts.Count}");
        shell.WriteLine($"Top {list.Count} by live count{(filter != null ? $" (filter: '{filter}')" : "")} (note: mostly static map, use 'diff' for leaks):");
        shell.WriteLine("   count  prototype");
        foreach (var kv in list)
            shell.WriteLine($"{kv.Value,8}  {kv.Key}");
    }

    private Dictionary<string, int> Count()
    {
        var counts = new Dictionary<string, int>();
        var query = _entManager.AllEntityQueryEnumerator<MetaDataComponent>();
        while (query.MoveNext(out _, out var meta))
        {
            var proto = meta.EntityPrototype?.ID ?? "(no prototype)";
            counts.TryGetValue(proto, out var c);
            counts[proto] = c + 1;
        }
        return counts;
    }

    private static (int top, string? filter) ParseTopFilter(string[] args, int start)
    {
        var top = 30;
        string? filter = null;
        if (args.Length > start)
        {
            if (int.TryParse(args[start], out var n))
            {
                top = n;
                if (args.Length > start + 1)
                    filter = args[start + 1];
            }
            else
            {
                filter = args[start];
            }
        }
        return (top, filter);
    }
}
