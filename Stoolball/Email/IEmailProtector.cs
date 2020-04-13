namespace Stoolball.Email
{
    public interface IEmailProtector
    {
        string ProtectEmailAddresses(string html, bool userIsAuthenticated);
    }
}