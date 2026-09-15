using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stoolball.Testing.UnitTests
{
    /// <summary>
    /// An xUnit class fixture providing Stoolball.Testing's factories (and their dependencies, e.g. <see cref="Randomiser"/>)
    /// via the same DI registrations <c>SeedDataGenerator</c> itself uses (<see cref="ServiceCollectionExtensions.AddSeedDataGenerator"/>),
    /// so tests don't need to construct a factory's dependency chain by hand. One instance is shared across all tests in a
    /// class.
    /// </summary>
    public class TestServicesFixture
    {
        private readonly IServiceProvider _services = new ServiceCollection().AddSeedDataGenerator().BuildServiceProvider();

        public T Get<T>() where T : notnull => _services.GetRequiredService<T>();
    }
}
