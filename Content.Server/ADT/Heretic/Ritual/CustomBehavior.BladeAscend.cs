using Content.Shared.Heretic.Prototypes;

namespace Content.Server.Heretic.Ritual;

public sealed partial class RitualBladeAscendBehavior : RitualSacrificeBehavior
{
    public override bool Execute(RitualData args, out string? outstr)
    {
        // Пока просто используем базовую логику жертвоприношения
        // В будущем можно добавить проверку на обезглавливание
        return base.Execute(args, out outstr);
    }
}
