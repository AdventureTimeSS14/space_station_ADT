using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.Paper;
using Content.Shared.PDA;

namespace Content.Shared.DocumentPrinter;
public sealed class DocumentPrinterSystem : EntitySystem
{
    const int TIME_YEAR_SPACE_STATION_ADT = 544;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DocumentPrinterComponent, PrintingDocumentEvent>(OnPrinting);
    }

    public void OnPrinting(EntityUid uid, DocumentPrinterComponent component, PrintingDocumentEvent args)
    {
        //coef for YEAR 544
        if (!TryComp<PaperComponent>(args.Paper, out var paperComponent)) return;
        if (!TryComp<InventoryComponent>(args.Actor, out var inventoryComponent)) return;

        string text = paperComponent.Content;
        MetaDataComponent? meta_id = null;
        PdaComponent? pda = null;
        foreach (var slot in inventoryComponent.Containers)
        {
            if (slot.ID == "id")//for checking only PDA
            {
                TryComp(slot.ContainedEntity, out pda);
                TryComp<ItemSlotsComponent>(slot.ContainedEntity, out var itemslots);
                if (itemslots is not null)
                    TryComp(itemslots.Slots["PDA-id"].Item, out meta_id);
                break;
            }
        }
        if (pda?.StationName is not null)
            text = text.Replace("Station XX-000", pda.StationName);
        DateTime time = DateTime.UtcNow;
        text = text.Replace("$time$", $"{time.AddYears(TIME_YEAR_SPACE_STATION_ADT).AddHours(4)}");
        if (meta_id is null)
        {
            text = text.Replace("$name$", "");
            text = text.Replace("$job$", "");
        }
        else
        {
            int startIndex = meta_id.EntityName.IndexOf("("); int endIndex = meta_id.EntityName.IndexOf(")");
            if (startIndex.Equals(-1) || startIndex.Equals(-1))
            {
                text = text.Replace("$name$", "");
                text = text.Replace("$job$", "");
            }
            else
            {
                string id_card_word = "ID карта ";
                text = text.Replace("$name$", meta_id.EntityName.Replace(id_card_word, "").Substring(0, startIndex - id_card_word.Length - 2));
                text = text.Replace("$job$", meta_id.EntityName.Substring(startIndex + 1, endIndex - startIndex - 1));
            }
        }
        paperComponent.Content = text;
        // if (!TryComp<MetaDataComponent>(args.Actor, out var comp)) return; // was for test, STFU JUST LEAVE IT HERE
    }
}

//(C) Korol_Charodey
