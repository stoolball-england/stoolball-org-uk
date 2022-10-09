namespace Stoolball.Email
{
    public interface IContactDetailsParser
    {
        string? ParseFirstEmailAddress(string html);
        string? ParseFirstPhoneNumber(string html);
    }
}