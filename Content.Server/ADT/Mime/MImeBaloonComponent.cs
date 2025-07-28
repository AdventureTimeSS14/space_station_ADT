namespace Content.Server.ADT.Mime;

[RegisterComponent]
public sealed partial class MimeBaloonComponent : Component
{
    public List<string> ListPrototypesBaloon = ["BalloonNT", "BalloonCorgi", "BalloonSyn"];

    [DataField, AutoNetworkedField]
    public EntityUid? Action;
}