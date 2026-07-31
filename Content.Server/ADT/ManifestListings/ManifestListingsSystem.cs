using System.Linq;
using System.Text;
using Content.Shared.ADT.ManifestListings;
using Content.Shared.Actions.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Store;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.ADT.ManifestListings;

public sealed class ManifestListingsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindComponent, ListingPurchasedEvent>(OnPurchase);

        SubscribeLocalEvent<MindListingsComponent, PrependObjectivesSummaryTextEvent>(OnPrepend);
    }

    private void OnPurchase(Entity<MindComponent> ent, ref ListingPurchasedEvent args)
    {
        var listings = EnsureComp<MindListingsComponent>(ent);

        var data = args.Data;
        if (!listings.Listings.TryGetValue(args.Store.Id, out var list))
        {
            list = new List<ManifestPurchaseRecord>();
            listings.Listings.Add(args.Store.Id, list);
        }

        var record = list.FirstOrDefault(x => x.Data.ID == data.ID);
        if (record == null)
        {
            record = new ManifestPurchaseRecord
            {
                Data = data,
            };
            list.Add(record);
        }

        record.Amount++;

        foreach (var (currency, amount) in args.Cost)
        {
            if (!record.Spent.TryAdd(currency, amount))
                record.Spent[currency] += amount;
        }
    }

    private void OnPrepend(Entity<MindListingsComponent> ent, ref PrependObjectivesSummaryTextEvent args)
    {
        var entries = new StringBuilder();
        var totalSpent = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>();

        foreach (var list in ent.Comp.Listings.Values)
        {
            var upgrades = new HashSet<string>();
            foreach (var record in list)
            {
                if (record.Data.ProductUpgradeId is { } upgradeId)
                    upgrades.Add(upgradeId.Id);
            }

            foreach (var record in list)
            {
                if (record.Amount <= 0 || upgrades.Contains(record.Data.ID))
                    continue;

                var amount = record.Amount;
                var spent = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>(record.Spent);

                if (record.Data.ProductUpgradeId is { } upgradeId)
                {
                    var upgrade = list.FirstOrDefault(x => x.Data.ID == upgradeId.Id);
                    if (upgrade != null)
                    {
                        amount += upgrade.Amount;

                        foreach (var (currency, value) in upgrade.Spent)
                        {
                            if (!spent.TryAdd(currency, value))
                                spent[currency] += value;
                        }
                    }
                }

                foreach (var (currency, value) in spent)
                {
                    if (!totalSpent.TryAdd(currency, value))
                        totalSpent[currency] += value;
                }

                entries.Append(BuildEntry(ent.Comp, record.Data, amount, spent));
            }
        }

        if (entries.Length == 0)
            return;

        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString("manifest-listing-entry-start", ("spent", FormatCurrency(totalSpent))));
        sb.AppendLine();
        sb.AppendLine(entries.ToString());

        args.Text += sb.ToString();
    }

    private string BuildEntry(
        MindListingsComponent comp,
        ListingData data,
        int amount,
        Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> spent)
    {
        GetSprite(comp, data, out var sprite, out var state);

        var info = Loc.GetString("manifest-listing-entry-info",
            ("name", GetName(data)),
            ("spent", FormatCurrency(spent)));

        info = info.Replace("\"", string.Empty).Replace("'", string.Empty);

        return Loc.GetString("manifest-listing-entry-listing",
            ("sprite", sprite),
            ("state", state),
            ("info", info),
            ("amount", amount));
    }

    private string GetName(ListingData data)
    {
        if (data.Name != null)
            return Loc.GetString(data.Name);

        if (data.ProductEntity != null)
            return Loc.GetString(_proto.Index(data.ProductEntity.Value).Name);

        if (data.ProductAction != null)
            return Loc.GetString(_proto.Index(data.ProductAction.Value).Name);

        return Loc.GetString("manifest-listing-entry-unknown");
    }

    private void GetSprite(MindListingsComponent comp, ListingData data, out string sprite, out string state)
    {
        state = string.Empty;

        switch (data.Icon)
        {
            case SpriteSpecifier.Texture tex:
                sprite = tex.TexturePath.ToString();
                if (!sprite.StartsWith("/Textures/"))
                    sprite = $"/Textures/{sprite}";
                return;

            case SpriteSpecifier.Rsi rsi:
                sprite = rsi.RsiPath.ToString();
                state = rsi.RsiState;
                return;
        }

        if (data.ProductEntity != null)
        {
            sprite = data.ProductEntity.Value;
            return;
        }

        if (data.ProductAction != null && TryGetActionIcon(data.ProductAction.Value, out var actionSprite, out var actionState))
        {
            sprite = actionSprite;
            state = actionState;
            return;
        }

        sprite = comp.DefaultTexture.TexturePath.ToString();
    }

    private string FormatCurrency(Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2> amounts)
    {
        var sb = new StringBuilder();

        foreach (var (currencyId, amount) in amounts)
        {
            if (amount <= 0 || !_proto.TryIndex(currencyId, out var currency))
                continue;

            if (sb.Length > 0)
                sb.Append(", ");

            sb.Append(Loc.GetString("manifest-listing-currency",
                ("amount", amount.ToString()),
                ("currency", Loc.GetString(currency.DisplayName))));
        }

        return sb.Length > 0 ? sb.ToString() : Loc.GetString("manifest-listing-free");
    }

    private bool TryGetActionIcon(EntProtoId proto, out string sprite, out string state)
    {
        sprite = string.Empty;
        state = string.Empty;

        if (!_proto.Index(proto).TryGetComponent("Action", out ActionComponent? actionComp) || actionComp.Icon == null)
            return false;

        switch (actionComp.Icon)
        {
            case SpriteSpecifier.Texture tex:
                sprite = tex.TexturePath.ToString();
                if (!sprite.StartsWith("/Textures/"))
                    sprite = $"/Textures/{sprite}";
                return true;

            case SpriteSpecifier.Rsi rsi:
                sprite = rsi.RsiPath.ToString();
                state = rsi.RsiState;
                return true;

            default:
                return false;
        }
    }
}
