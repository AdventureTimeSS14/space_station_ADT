using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems
{
    public sealed class GondolaSpeakingSystem : EntitySystem
    {
        private static readonly Regex RegexLoneI = new(@"(?<=\ )i(?=[\ \.\?]|$)");

        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<GondolaSpeakingComponent, AccentGetEvent>(OnAccent);
        }

        public string Accentuate(string message)
        {
            var words = message.ToLower().Split();

            return Loc.GetString("...");
        }

        private void OnAccent(EntityUid uid, GondolaSpeakingComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message);
        }
    }
}
