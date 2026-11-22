using System.Linq;
using Content.Client.ADT.Lobby.UI;
using Content.Shared.ADT.BodyTypes;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private BodyTypeWindow? _bodyTypeWindow;

    private void InitializeBodyTypes()
    {
        BodyTypeButton.OnToggled += args =>
        {
            if (args.Pressed)
                OpenBodyTypeWindow();
            else
                _bodyTypeWindow?.Close();
        };
    }

    private void OpenBodyTypeWindow()
    {
        if (Profile == null)
            return;

        _bodyTypeWindow?.Close();
        _bodyTypeWindow = new BodyTypeWindow();
        _bodyTypeWindow.OnBodyTypeSelected += SetBodyType;
        _bodyTypeWindow.OnClose += () =>
        {
            BodyTypeButton.Pressed = false;
            _bodyTypeWindow = null;
        };

        _bodyTypeWindow.LoadPreview(Profile, JobOverride, ShowClothes.Pressed);
        _bodyTypeWindow.OpenCentered();
    }

    private void SetBodyType(ProtoId<BodyTypePrototype>? bodyType)
    {
        if (Profile == null)
            return;

        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithBodyType(bodyType));
        UpdateBodyTypeControls();
        ReloadProfilePreview();
    }

    private void UpdateBodyTypeControls()
    {
        if (Profile == null)
            return;

        var bodyTypes = BodyTypeWindow.GetBodyTypes(_prototypeManager, Profile.Species);

        if (Profile.Appearance.BodyType is { } current && bodyTypes.All(bodyType => bodyType.ID != current))
            Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithBodyType(null));

        BodyTypeContainer.Visible = bodyTypes.Count > 0;
        BodyTypeButton.Text = Profile.Appearance.BodyType is { } id && _prototypeManager.TryIndex(id, out var proto)
            ? Loc.GetString(proto.Name)
            : Loc.GetString("body-type-default");

        if (bodyTypes.Count == 0)
            _bodyTypeWindow?.Close();
    }
}
