using System.Diagnostics.CodeAnalysis;
using System.Text;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Roles
{
    /// <summary>
    /// Requires the character to be of a specified sex.
    /// </summary>
    [UsedImplicitly]
    [Serializable, NetSerializable]
    public sealed partial class SexRequirement : JobRequirement
    {
        [DataField(required: true)]
        public HashSet<Sex> AllowedSex = new();

        /// <summary>
        /// Проверка, соответствует ли персонаж требуемому полу
        /// </summary>
        /// <param name="entManager">Менеджер сущностей</param>
        /// <param name="protoManager">Менеджер прототипов</param>
        /// <param name="profile">Профиль персонажа</param>
        /// <param name="playTimes">Время игры</param>
        /// <param name="reason">Причина отказа, если не прошел проверку</param>
        /// <returns>Возвращает true, если проверка пройдена, иначе false</returns>
        public override bool Check(IEntityManager entManager,
            IPrototypeManager protoManager,
            HumanoidCharacterProfile? profile,
            IReadOnlyDictionary<string, TimeSpan> playTimes,
            [NotNullWhen(false)] out FormattedMessage? reason)
        {
            reason = new FormattedMessage();

            if (profile is null)
                return true;

            var sb = new StringBuilder();
            sb.Append("[color=yellow]");

            // Преобразование коллекции AllowedSex в строку для отображения
            sb.Append(string.Join(", ", AllowedSex));

            sb.Append("[/color]");

            if (!Inverted)
            {
                reason = FormattedMessage.FromMarkupPermissive($"{Loc.GetString("role-timer-whitelisted-sex")}\n{sb}");

                if (!AllowedSex.Contains(profile.Sex))
                    return false;
            }
            else
            {
                if (AllowedSex.Contains(profile.Sex))
                    return false;
            }

            return true;
        }
    }
}
