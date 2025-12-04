using System.Text;
using Content.Server.ADT.Speech.Components;
using Content.Shared.ADT.Speech.EntitySystems;
using Content.Shared.Speech;
using Content.Shared.StatusEffect;
using Robust.Shared.Random;

namespace Content.Server.ADT.Speech.EntitySystems
{
    public sealed class WeaknessSystem : SharedWeaknessSystem
    {
        [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<WeaknessAccentComponent, AccentGetEvent>(OnAccent);
        }

        public override void DoWeakness(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
        {
            if (!Resolve(uid, ref status, false))
                return;

            _statusEffectsSystem.TryAddStatusEffect<WeaknessAccentComponent>(uid, WeaknessKey, time, refresh, status);
        }

        private void OnAccent(EntityUid uid, WeaknessAccentComponent component, AccentGetEvent args)
        {
            args.Message = Accentuate(args.Message, component);
        }

        public string Accentuate(string message, WeaknessAccentComponent component)
        {
            if (string.IsNullOrEmpty(message))
                return message;

            var words = message.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return message;

            var finalMessage = new StringBuilder();

            for (var i = 0; i < words.Length; i++)
            {
                finalMessage.Append(words[i]);

                if (i < words.Length - 1)
                {
                    if (_random.Prob(component.MatchRandomProb))
                    {
                        finalMessage.Append("...");
                    }
                    finalMessage.Append(" ");
                }
                else
                {
                    finalMessage.Append("...");
                }
            }

            return finalMessage.ToString();
        }
    }
}
