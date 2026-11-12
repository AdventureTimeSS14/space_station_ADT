using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.Virology.Pandemic;
using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Virology.UI;

[UsedImplicitly]
public sealed class ADTPandemicBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private ADTPandemicWindow? _window;

    public ADTPandemicBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ADTPandemicWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _window.EjectButton.OnPressed += _ => SendMessage(new ADTPandemicEjectBeakerMessage(false));
        _window.ClearButton.OnPressed += _ => SendMessage(new ADTPandemicEjectBeakerMessage(true));
        _window.OnCreateCulture += index => SendMessage(new ADTPandemicCreateCultureMessage(index));
        _window.OnCreateVaccine += index => SendMessage(new ADTPandemicCreateVaccineMessage(index));
        _window.OnPrintForm += index => SendMessage(new ADTPandemicPrintFormMessage(index));
        _window.OnRequestSymptoms += index => SendMessage(new ADTPandemicRequestSymptomsMessage(index));
        _window.OnAddSymptom += (index, symptom) => SendMessage(new ADTPandemicAddSymptomMessage(index, symptom));
        _window.OnRemoveSymptom += (index, symptom) => SendMessage(new ADTPandemicRemoveSymptomMessage(index, symptom));
        _window.OnRenameDisease += OpenRenameDialog;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ADTPandemicBoundUserInterfaceState cast)
            return;

        _window?.UpdateState(cast);
    }

    private void OpenRenameDialog(int index)
    {
        var dialog = new DialogWindow(
            Loc.GetString("adt-pandemic-rename-title"),
            new List<QuickDialogEntry>
            {
                new QuickDialogEntry("name", QuickDialogEntryType.ShortText, Loc.GetString("adt-pandemic-rename-prompt")),
            });

        dialog.OnConfirmed += responses =>
        {
            if (!responses.TryGetValue("name", out var name) || name.Length == 0)
                return;

            SendMessage(new ADTPandemicRenameDiseaseMessage(index, name));
        };

        dialog.OpenCentered();
    }
}
