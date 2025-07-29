namespace Content.Server.ADT.Mime;

[RegisterComponent]
public sealed partial class MimeBaloonComponent : Component
{
    public List<string> ListPrototypesBaloon = ["BalloonNT", "BalloonCorgi"];

    [DataField, AutoNetworkedField]
    public EntityUid? Action;
}
