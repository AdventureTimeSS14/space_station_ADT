using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.InconnuOS.Components;
using Content.Shared.Emp;
using Content.Shared.Power;
using Robust.Shared.Timing;

namespace Content.Server.ADT.InconnuOS;

public sealed partial class ADTOsSystem : SharedADTOsSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTOperatingSystemComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTOperatingSystemComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ADTOperatingSystemComponent, EmpPulseEvent>(OnEmpPulse);

        InitializeUi();
        InitializeDrives();
        InitializeShell();
    }

    private void OnMapInit(Entity<ADTOperatingSystemComponent> ent, ref MapInitEvent args)
    {
        var comp = ent.Comp;

        if (string.IsNullOrWhiteSpace(comp.MachineName))
            comp.MachineName = GenerateMachineName(ent);

        if (string.IsNullOrEmpty(comp.WorkingDirectory))
            comp.WorkingDirectory = OsPath.GetRoot(comp.Drive);

        InstallDefaultFiles(ent);
        FilterApps(ent);
    }

    private void OnPowerChanged(Entity<ADTOperatingSystemComponent> ent, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        PowerOff(ent);
    }

    public void PowerOn(Entity<ADTOperatingSystemComponent> ent)
    {
        var comp = ent.Comp;

        if (comp.Running)
            return;

        comp.Running = true;
        comp.Crashed = false;
        comp.BootedAt = _timing.CurTime;
    }

    public void PowerOff(Entity<ADTOperatingSystemComponent> ent)
    {
        var comp = ent.Comp;

        comp.Running = false;
        comp.Crashed = false;
        comp.BootedAt = TimeSpan.Zero;

        _ui.CloseUi(ent.Owner, ADTComputerUiKey.Key);
    }

    private void OnEmpPulse(Entity<ADTOperatingSystemComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        Crash(ent);
    }

    public void Crash(Entity<ADTOperatingSystemComponent> ent)
    {
        if (ent.Comp.Crashed || !ent.Comp.Running)
            return;

        ent.Comp.Crashed = true;
        UpdateUiState(ent);
    }

    public void Reboot(Entity<ADTOperatingSystemComponent> ent)
    {
        ent.Comp.Running = true;
        ent.Comp.Crashed = false;
        ent.Comp.BootedAt = _timing.CurTime;

        RestoreKernel(ent);

        UpdateUiState(ent);
    }

    private static string GenerateMachineName(EntityUid uid)
    {
        return $"FILO-{uid.Id:X4}";
    }

    private void FilterApps(Entity<ADTOperatingSystemComponent> ent)
    {
        var comp = ent.Comp;

        for (var i = comp.Apps.Count - 1; i >= 0; i--)
        {
            if (!Prototypes.TryIndex(comp.Apps[i], out var app))
            {
                Log.Error($"Компьютер {ToPrettyString(ent)} ссылается на несуществующее приложение {comp.Apps[i].Id}.");
                comp.Apps.RemoveAt(i);
                continue;
            }

            if (app.RequiresComponent == null)
                continue;

            if (!_factory.TryGetRegistration(app.RequiresComponent, out var registration))
            {
                Log.Error($"Приложение {app.ID} требует несуществующий компонент {app.RequiresComponent}.");
                comp.Apps.RemoveAt(i);
                continue;
            }

            if (!HasComp(ent.Owner, registration.Type))
                comp.Apps.RemoveAt(i);
        }
    }

    partial void InitializeUi();

    partial void InitializeDrives();

    partial void InitializeShell();
}
