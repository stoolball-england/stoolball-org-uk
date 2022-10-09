namespace Stoolball.Security
{
    public interface IDataRedactor
    {
        string? RedactAll(string? unredacted);
        string? RedactPersonalData(string? unredacted);
    }
}
