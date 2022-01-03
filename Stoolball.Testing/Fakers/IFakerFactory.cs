using Bogus;

namespace Stoolball.Testing.Fakers
{
    public interface IFakerFactory<T> where T : class
    {
        Faker<T> Create();
    }
}