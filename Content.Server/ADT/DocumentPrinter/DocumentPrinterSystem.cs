using Content.Server.GameTicking;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Inventory;
using Content.Shared.Paper;
using Content.Shared.PDA;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.DocumentPrinter;

public sealed class DocumentPrinterSystem : EntitySystem
{
    const int TIME_YEAR_SPACE_STATION_ADT = 544;

    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DocumentPrinterComponent, PrintingDocumentEvent>(OnPrinting);
        SubscribeLocalEvent<DocumentPrinterComponent, GetVerbsEvent<AlternativeVerb>>(AddVerbOnOff);
    }

    public void AddVerbOnOff(EntityUid uid, DocumentPrinterComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess) return;
        AlternativeVerb verb = new();
        if (component.IsOnAutocomplite)
        {
            verb.Text = "Выкл.";
            verb.Act = () =>
        {
            component.IsOnAutocomplite = false;
            _audioSystem.PlayPvs(component.SwitchSound, uid);
        };
        }
        else
        {
            verb.Text = "Вкл.";
            verb.Act = () =>
        {
            component.IsOnAutocomplite = true;
            _audioSystem.PlayPvs(component.SwitchSound, uid);
        };
        }
        args.Verbs.Add(verb);
    }

    public void OnPrinting(EntityUid uid, DocumentPrinterComponent component, PrintingDocumentEvent args)
    {
        //coef for YEAR 544
        if (!TryComp<PaperComponent>(args.Paper, out var paperComponent)) return;
        if (!TryComp<InventoryComponent>(args.Actor, out var inventoryComponent)) return;

        string text = paperComponent.Content;

        if (component.IsOnAutocomplite)
        {
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
            DateTime time = DateTime.UtcNow.AddYears(TIME_YEAR_SPACE_STATION_ADT).AddHours(3);
            text = text.Replace("$time$", $"{_gameTiming.CurTime.Subtract(_ticker.RoundStartTimeSpan).ToString("hh\\:mm\\:ss")} | {(time.Day < 10 ? $"0{time.Day}" : time.Day)}.{(time.Month < 10 ? $"0{time.Month}" : time.Month)}.{time.Year}");
            if (pda is not null)
            {
                if (pda?.StationName is not null)
                {
                    text = text.Replace("Station XX-000", pda.StationName);
                }
                if (meta_id is null)
                {
                    text = text.Replace("$name$", "");
                    text = text.Replace("$job$", "");
                }
                else
                {
                    int startIndex = meta_id.EntityName.IndexOf("("); int endIndex = meta_id.EntityName.IndexOf(")");
                    if (startIndex.Equals(-1) || endIndex.Equals(-1))
                    {
                        text = text.Replace("$name$", "");
                        text = text.Replace("$job$", "");
                    }
                    else
                    {
                        string id_card_word = "ID карта ";
                        if (startIndex - id_card_word.Length - 2 > 0)
                            text = text.Replace("$name$", meta_id.EntityName.Replace(id_card_word, "").Substring(0, startIndex - id_card_word.Length - 2));
                        else
                            text = text.Replace("$name$", "");
                        if (startIndex + 1 != endIndex)
                            text = text.Replace("$job$", meta_id.EntityName.Substring(startIndex + 1, endIndex - startIndex - 1));

                        else
                            text = text.Replace("$name$", "");
                    }
                }
                paperComponent.Content = text;
                // if (!TryComp<MetaDataComponent>(args.Actor, out var comp)) return; // was for test, STFU JUST LEAVE IT HERE
            }
            else
            {
                text = text.Replace("$name$", "");
                text = text.Replace("$job$", "");
                paperComponent.Content = text;
            }
        }
        else
        {
            text = text.Replace("$time$", "");
            text = text.Replace("$name$", "");
            text = text.Replace("$job$", "");
            paperComponent.Content = text;
        }
    }
}

/*                                         ████▒
                             ░             ████▓             ░
                          █████   ░░▒▓▓▓███████████▓▓▒▒░    █████
                          ░█████████████████████████████████████
                       ░▒████████████▓▓▒▒░░  ▓███████████████████▓▒
             ░▓█▓   ▒█████████▓▒░            ▓███████████████████████▓░  ░▓█▓░
            ░█████████████▒                  ▓████████████████████████████████░
              ░███████▓░               ░▒▓▓███     ░░▓██████████████████████
             ▒██████▒      ░    ▓██▓▓▓████████          ░▒█████████████▓█████▒
           ▒██████░       ░███▒█████▓▒ ▒██████             ░▒███████████▒░▓████▓
    ▒█▓░ ░██████░           ▒██████████  █████                ░▓█████████▓ ░█████  ░▓█░
   ▒██████████▓          ▒███▓ ████████░ █████                   ▓█████████░ ██████████░
      ░██████░          ██████▒ ███████▓ ▒███▓                    ▒█████████▒ ██████░
       █████░      ░   ░███████▒ ░▒▓████▒░ ░░▓                    ▒██████████░ █████
      █████▒     ░█▒▓█  ██████████▒ ░▒█████▒██                    ████████████  █████
     ▓█████      ░█░  █ ▒███████████▓▓████████                   ▒████████████▓ ▒████░
     █████▒       ░▒░▒█▓██████████████████████         ░█▒░      ▓█████████████  ████▓
▓▓▓▓▓█████       ▒▓ ███▒▒▓█████▓░░   ░ ░▒█████         ▓████     ██████████████  █████▒▓▓▓      ©Korol_Charodey
██████████░    ████▒█████▓████░ ▒ ▒░░▒ ▒ ░▒███        ▓█████░    ██████████████  █████████
     █████▒  ░██▓  ▓███████████ ▓░▒░░▒ ▓ █████     ▒████████     ▒▒▒▓██████████  █████
     ▒█████  ██▓       ░███████ ▓ ▒░▒▒ ▓ ▓███▒ ▓█████████████        █████████▓ ▒████
      █████░ ██░     ░▒▓███████░░ ░░░▒ ░ ▓░░▒█       ░░░░▒▒▒░        █████████  █████
      ░█████░▓██      ▒██████████▓▓░  ▓▓█▒▒▓▒███░██▓▓▒▓▒▒░░         █████████  █████
   ▓█████████ ░██▓░  ░▓███████▒░████▒ █████▒▒██▓ █▓▒░▒▒▓█████▓    ▓█████████░ ▓████████▒
   ░████▓█████░ ▒███████████▒░▒░▓▓███████▓▒▒██           ██████▓███████████  █████▓████
         ░█████▓         ▓   ███▓▒██████▒█████       ▒░▒░▓███████████████▒ ░█████    ░
           ▒█████▒       ▓░░█░▒▓██▓▓▓▓█▓█▓██▓▓      ▓ ▒ ▒██████████████▓░░█████░
             ▒█████▓      ▓░░▓▒▒██░▒ ░▒ ░░░▒░▒ ░▒  ▓   ██████████████▓░▒█████▒
              ████████▒           ▓▓▒░▓░▒░▒▒▒░  ▒  ▒░▒▒████████████▓▒▓██████▓
            ▓████▓▓██████▓░        ░  ░ ░  ░ ▓░ ▒███████████████████████▓█████▒
              ▒▒    ░▓███████▓▒              ▓███████████████████████▒░    ▒░
                        ░▓██████████▓▒░░     ▓███████████████████▒░
                         ░█████████████████████████████████▓█████
                         █████     ░░▒▒▓▓███████▓█▓▓▒▒░░     ████▓
                                           ████▒   */
