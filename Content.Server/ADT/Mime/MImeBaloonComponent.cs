namespace Content.Server.ADT.Mime;

[RegisterComponent]
public sealed partial class MimeBaloonComponent : Component
{
    public List<string> ListPrototypesBaloon = ["ADTBalloon", "ADTBalloonNT", "ADTBalloonCorgi", "ADTBalloonClown"];

    [DataField, AutoNetworkedField]
    public EntityUid? Action;
}
