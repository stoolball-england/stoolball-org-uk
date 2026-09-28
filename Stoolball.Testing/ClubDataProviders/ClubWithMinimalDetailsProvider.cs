namespace Stoolball.Testing.ClubDataProviders
{
    /// <summary>
    /// Ensures there's always a club with minimal details.
    /// </summary>
    internal class ClubWithMinimalDetailsProvider : BaseClubDataProvider
    {
        private readonly ClubFactory _clubFactory;

        internal ClubWithMinimalDetailsProvider(ClubFactory clubFactory)
        {
            _clubFactory = clubFactory ?? throw new ArgumentNullException(nameof(clubFactory));
        }

        internal override IEnumerable<Club> CreateClubs(TestData readOnlyTestData)
        {
            return [_clubFactory.CreateFaker().Generate()];
        }
    }
}
