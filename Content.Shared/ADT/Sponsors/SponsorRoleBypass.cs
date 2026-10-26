namespace Content.Shared.ADT.Sponsors;

[Flags]
public enum SponsorRoleBypass : byte
{
    None = 0,
    Jobs = 1 << 0,
    Antags = 1 << 1,
}
