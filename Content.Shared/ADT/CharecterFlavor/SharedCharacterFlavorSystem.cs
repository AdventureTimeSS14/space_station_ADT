// Inspired by Nyanotrasen
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.CharecterFlavor;

public abstract class SharedCharecterFlavorSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] protected readonly IConfigurationManager ConfigManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterFlavorComponent, GetVerbsEvent<ExamineVerb>>(OnOpenUi);
    }

    private void OnOpenUi(Entity<CharacterFlavorComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        var detailsRange = _examine.IsInDetailsRange(args.User, ent);

        var user = args.User;
        var verb = new ExamineVerb
        {
            Act = () => OpenFlavor(user, ent.Owner),
            Text = Loc.GetString("detail-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("detail-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/examine.svg.192dpi.png"))
        };

        args.Verbs.Add(verb);
    }

    protected virtual void OpenFlavor(EntityUid actor, EntityUid target)
    {
        // Реализация в серверной/клиентской системе
    }

    /// <summary>
    /// Полная валидация URL с проверкой домена (для сервера)
    /// </summary>
    protected bool IsValidHeadshotUrl(string url, string? allowedDomain = null)
    {
        // Используем строковую проверку для совместимости
        return HeadshotHashHelper.IsValidHeadshotUrl(url, allowedDomain);
    }

    /// <summary>
    /// Базовая валидация URL (без проверки домена - для клиента)
    /// Использует простую строковую проверку без Uri для совместимости с sandbox клиента
    /// </summary>
    protected static bool IsValidHeadshotUrlFormat(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (url.Length > 500)
            return false;

        // Простая проверка: URL должен начинаться с http:// или https://
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}

/// <summary>
/// Вспомогательные методы для работы с headshot (доступны везде)
/// </summary>
public static class HeadshotHashHelper
{
    /// <summary>
    /// Вычисление простого хэша байтового массива для кэширования
    /// </summary>
    public static int ComputeHash(byte[] data)
    {
        unchecked
        {
            int hash = 17;
            foreach (var b in data)
            {
                hash = hash * 31 + b;
            }
            return hash;
        }
    }

    /// <summary>
    /// Полная валидация URL с проверкой домена (статическая версия для использования вне систем)
    /// Использует простую строковую проверку без Uri для совместимости с sandbox клиента
    /// </summary>
    public static bool IsValidHeadshotUrl(string url, string? allowedDomain = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (url.Length > 500)
            return false;

        // Простая проверка: URL должен начинаться с http:// или https://
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return false;

        // Проверка домена (если указан) - простая строковая проверка
        if (!string.IsNullOrEmpty(allowedDomain))
        {
            // Извлекаем хост из URL (между :// и первым /)
            var schemeEnd = url.IndexOf("://", StringComparison.OrdinalIgnoreCase);
            if (schemeEnd < 0)
                return false;

            var pathStart = url.IndexOf('/', schemeEnd + 3);
            var host = pathStart > 0 ? url.Substring(schemeEnd + 3, pathStart - schemeEnd - 3) : url.Substring(schemeEnd + 3);

            if (!host.Equals(allowedDomain, StringComparison.OrdinalIgnoreCase) &&
                !host.EndsWith("." + allowedDomain, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
