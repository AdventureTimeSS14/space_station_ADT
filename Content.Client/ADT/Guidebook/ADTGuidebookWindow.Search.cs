using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Kitchen;
using Content.Shared.Research.Prototypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.ADT.Guidebook;

public sealed partial class ADTGuidebookWindow
{
    private const int SearchBatchSize = 8;
    private const int SearchResultLimit = 60;
    private static readonly Regex Comments = new(@"<!--[\s\S]*?-->");
    private static readonly Regex TextLinks = new("\\[textlink=\"([^\"]*)\"[^\\]]*\\]");
    private static readonly Regex Markup = new(@"<[^>]*>|\[[^\]]*\]");
    private static readonly Regex Whitespace = new(@"\s+");
    private static readonly Regex Embeds = new(@"<(?<tag>Guide\w+)\b(?<attributes>[^>]*)/?>");
    private static readonly Regex Attributes = new("(?<name>\\w+)=\"(?<value>[^\"]*)\"");
    private readonly Dictionary<string, Document> _documents = new();
    private readonly Dictionary<string, string> _embedSearchText = new();

    private void QueueSearch()
    {
        if (_settingSearch)
            return;

        CancelSearch();
        var query = SearchBar.Text.Trim();
        ClearSearchButton.Disabled = query.Length == 0;
        if (query.Length == 0)
        {
            if (_page?.Kind == PageKind.Search)
                Navigate(_searchOrigin is { } origin ? origin with { } : new Page(PageKind.Home));
            return;
        }

        if (_page?.Kind != PageKind.Search)
            _searchOrigin = _page;

        _searchCancel = new CancellationTokenSource();
        Timer.Spawn(180, () =>
        {
            if (Disposed)
                return;

            if (_page is { Kind: PageKind.Search } page && page.Query == query)
            {
                CancelSearch();
                ClearControls(PageBody);
                ShowSearch(query);
            }
            else
            {
                Navigate(new Page(PageKind.Search, Query: query));
            }
        }, _searchCancel.Token);
    }

    private void ClearSearch()
    {
        CancelSearch();
        SearchBar.Clear();
    }

    private void ShowSearch(string query)
    {
        SetTitle(Loc.GetString("adt-guidebook-search-title"));
        SetDescription(Loc.GetString("adt-guidebook-searching", ("query", query)));
        var tokens = NormalizeSearch(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var ids = _visibleEntries.OrderBy(GetEntryName).ToList();
        var results = new List<SearchResult>();
        _searchCancel = new CancellationTokenSource();
        var token = _searchCancel.Token;
        ScanSearchBatch(query, tokens, ids, results, 0, token);
    }

    private void ScanSearchBatch(
        string query,
        string[] tokens,
        List<string> ids,
        List<SearchResult> results,
        int offset,
        CancellationToken token)
    {
        if (Disposed || token.IsCancellationRequested)
            return;

        var normalizedQuery = NormalizeSearch(query);
        var end = Math.Min(offset + SearchBatchSize, ids.Count);
        for (var i = offset; i < end; i++)
        {
            var id = ids[i];
            var document = GetDocument(id, true);
            var title = NormalizeSearch(GetEntryName(id));
            var score = 0;
            foreach (var word in tokens)
            {
                if (title.Contains(word, StringComparison.Ordinal))
                {
                    score += 20;
                }
                else if (document.SearchText.Contains(word, StringComparison.Ordinal))
                {
                    score++;
                }
                else
                {
                    score = -1;
                    break;
                }
            }

            if (score < 0)
                continue;

            if (title == normalizedQuery)
                score += 100;

            results.Add(new SearchResult(id, score));
        }

        if (end < ids.Count)
        {
            Timer.Spawn(1, () => ScanSearchBatch(query, tokens, ids, results, end, token), token);
            return;
        }

        ShowSearchResults(query, tokens, results);
    }

    private void ShowSearchResults(string query, string[] tokens, List<SearchResult> results)
    {
        SetDescription(Loc.GetString("adt-guidebook-search-results", ("query", query), ("count", results.Count)));
        if (results.Count == 0)
        {
            PageBody.AddChild(CreateText(Loc.GetString("adt-guidebook-search-empty")));
            return;
        }

        foreach (var result in results.OrderByDescending(result => result.Score).ThenBy(result => GetEntryName(result.Id)).Take(SearchResultLimit))
        {
            var id = result.Id;
            PageBody.AddChild(CreateSearchResult(
                id,
                GetSnippet(GetDocument(id), tokens),
                () => OpenSearchResult(id, query)));
        }

        if (results.Count > SearchResultLimit)
        {
            PageBody.AddChild(CreateText(Loc.GetString("adt-guidebook-search-limit", ("count", SearchResultLimit))));
        }

        var page = _page;
        UserInterfaceManager.DeferAction(() =>
        {
            if (!Disposed && page != null && ReferenceEquals(_page, page))
                ContentScroll.SetScrollValue(page.Scroll);
        });
    }

    private void OpenSearchResult(string id, string query)
    {
        OpenEntry(id);
        var match = _articleFilters.FirstOrDefault(filter => filter.CheckMatchesSearch(query));
        if (match is not Control control)
            return;

        if (FilterTools.Visible)
            FilterBar.Text = query;

        ScrollTo(control);
    }

    private Document GetDocument(string id, bool indexEmbeds = false)
    {
        if (_documents.TryGetValue(id, out var document))
            return indexEmbeds ? IndexDocumentEmbeds(id, document) : document;

        try
        {
            using var reader = _resources.ContentFileReadText(_entries[id].Text);
            var markup = reader.ReadToEnd();
            var withoutComments = Comments.Replace(markup, string.Empty);
            var plainText = GetPlainText(withoutComments);
            var preview = plainText;
            var firstLine = withoutComments.Split('\n').FirstOrDefault(line => line.TrimStart().StartsWith('#'));
            if (firstLine != null)
            {
                var heading = GetPlainText(firstLine);
                if (preview.StartsWith(heading, StringComparison.Ordinal))
                    preview = preview.Substring(heading.Length).Trim();
            }

            if (preview.Length > 160)
                preview = preview.Substring(0, 157).TrimEnd() + "...";
            if (preview.Length == 0)
                preview = Loc.GetString("adt-guidebook-read");

            document = new Document(markup, plainText, NormalizeSearch(plainText), preview, true);
        }
        catch (Exception e)
        {
            Logger.GetSawmill("adt.guidebook").Error($"Failed to index guidebook entry {id}: {e}");
            document = new Document(string.Empty, string.Empty, string.Empty, Loc.GetString("adt-guidebook-document-error"), false);
        }

        _documents.Add(id, document);
        return indexEmbeds ? IndexDocumentEmbeds(id, document) : document;
    }

    private Document IndexDocumentEmbeds(string id, Document document)
    {
        if (document.Indexed || !document.Valid)
            return document;

        var text = new StringBuilder(document.Text);
        try
        {
            foreach (Match embed in Embeds.Matches(Comments.Replace(document.Markup, string.Empty)))
            {
                var attributes = new Dictionary<string, string>();
                foreach (Match attribute in Attributes.Matches(embed.Groups["attributes"].Value))
                {
                    attributes[attribute.Groups["name"].Value] = attribute.Groups["value"].Value;
                }

                text.Append(' ').Append(attributes.GetValueOrDefault("Caption", string.Empty));
                text.Append(' ').Append(GetEmbedSearchText(embed.Groups["tag"].Value, attributes));
            }
        }
        catch (Exception e)
        {
            Logger.GetSawmill("adt.guidebook").Error($"Failed to index embedded guidebook data in {id}: {e}");
        }

        var plainText = GetPlainText(text.ToString());
        document = document with
        {
            Text = plainText,
            SearchText = NormalizeSearch(plainText),
            Indexed = true,
        };
        _documents[id] = document;
        return document;
    }

    private string GetEmbedSearchText(string tag, Dictionary<string, string> attributes)
    {
        var value = attributes.GetValueOrDefault("Group")
            ?? attributes.GetValueOrDefault("Discipline")
            ?? attributes.GetValueOrDefault("Entity")
            ?? attributes.GetValueOrDefault("Reagent")
            ?? attributes.GetValueOrDefault("Recipe")
            ?? attributes.GetValueOrDefault("Technology")
            ?? string.Empty;
        var key = $"{tag}:{value}";
        if (_embedSearchText.TryGetValue(key, out var cached))
            return cached;

        var text = new StringBuilder();
        switch (tag)
        {
            case "GuideEntityEmbed":
                AppendEntityText(text, value);
                break;
            case "GuideReagentEmbed":
                AppendReagentText(text, value);
                break;
            case "GuideReagentGroupEmbed":
            case "GuideADTReagentGroupEmbed":
                foreach (var reagent in _prototype.EnumeratePrototypes<ReagentPrototype>().Where(p => p.Group == value))
                {
                    AppendReagentText(text, reagent.ID);
                }
                break;
            case "GuideMicrowaveEmbed":
                if (_prototype.TryIndex<FoodRecipePrototype>(value, out var recipe))
                    AppendRecipeText(text, recipe);
                break;
            case "GuideMicrowaveGroupEmbed":
            case "GuideADTMicrowaveGroupEmbed":
                const int activeTypes = (int) (MicrowaveRecipeType.ADTMicrowave | MicrowaveRecipeType.ADTAssembler | MicrowaveRecipeType.MedicalAssembler | MicrowaveRecipeType.TribalOven);
                foreach (var microwave in _prototype.EnumeratePrototypes<FoodRecipePrototype>())
                {
                    if (microwave.Group == value && !microwave.HideInGuidebook && (microwave.RecipeType & activeTypes) != 0)
                        AppendRecipeText(text, microwave);
                }
                break;
            case "GuideADTCookingGroupEmbed":
                foreach (var cooking in _prototype.EnumeratePrototypes<ADTCookingRecipePrototype>())
                {
                    if (cooking.Group != value || cooking.HideInGuidebook || cooking.Result is not { } result)
                        continue;

                    AppendEntityText(text, result.Id);
                    foreach (var ingredient in cooking.Ingredients)
                    {
                        if (ingredient.Id is { } ingredientId)
                            AppendEntityText(text, ingredientId.Id);
                    }

                    foreach (var reagent in cooking.Reagents.Keys)
                    {
                        AppendReagentText(text, reagent.Id);
                    }
                }
                break;
            case "GuideTechnologyEmbed":
                if (_prototype.TryIndex<TechnologyPrototype>(value, out var technology))
                    text.Append(' ').Append(Loc.GetString(technology.Name));
                break;
            case "GuideTechDisciplineEmbed":
                foreach (var disciplineTechnology in _prototype.EnumeratePrototypes<TechnologyPrototype>().Where(p => p.Discipline.Id == value))
                {
                    text.Append(' ').Append(Loc.GetString(disciplineTechnology.Name));
                }
                break;
        }

        cached = text.ToString();
        _embedSearchText.Add(key, cached);
        return cached;
    }

    private void AppendEntityText(StringBuilder text, string id)
    {
        if (_prototype.TryIndex<EntityPrototype>(id, out var entity))
            text.Append(' ').Append(entity.Name).Append(' ').Append(entity.Description);
    }

    private void AppendReagentText(StringBuilder text, string id)
    {
        if (_prototype.TryIndex<ReagentPrototype>(id, out var reagent))
            text.Append(' ').Append(reagent.LocalizedName).Append(' ').Append(reagent.LocalizedDescription);
    }

    private void AppendRecipeText(StringBuilder text, FoodRecipePrototype recipe)
    {
        AppendEntityText(text, recipe.Result.Id);
        foreach (var ingredient in recipe.IngredientsSolids.Keys)
        {
            AppendEntityText(text, ingredient.Id);
        }

        foreach (var reagent in recipe.IngredientsReagents.Keys)
        {
            AppendReagentText(text, reagent.Id);
        }
    }

    private static string GetPlainText(string markup)
    {
        var text = TextLinks.Replace(markup, "$1");
        text = Markup.Replace(text, " ");
        text = text.Replace("#", string.Empty);
        return Whitespace.Replace(text, " ").Trim();
    }

    private static string NormalizeSearch(string text)
    {
        return Whitespace.Replace(text.ToLowerInvariant().Replace('ё', 'е'), " ").Trim();
    }

    private static string GetSnippet(Document document, string[] tokens)
    {
        var position = tokens.Select(token => document.SearchText.IndexOf(token, StringComparison.Ordinal))
            .Where(index => index >= 0).DefaultIfEmpty(0).Min();
        var start = Math.Min(Math.Max(position - 45, 0), document.Text.Length);
        var length = Math.Min(document.Text.Length - start, 210);
        return $"{(start > 0 ? "..." : string.Empty)}{document.Text.Substring(start, length)}{(start + length < document.Text.Length ? "..." : string.Empty)}";
    }

    private sealed record Document(string Markup, string Text, string SearchText, string Preview, bool Valid, bool Indexed = false);
    private sealed record SearchResult(string Id, int Score);
}
