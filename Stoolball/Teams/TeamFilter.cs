
namespace Stoolball.Teams
{
    public class TeamFilter : TeamListingFilter
    {
        /// <summary>
        /// Gets or sets whether to include teams that are in clubs
        /// </summary>
        public bool IncludeClubTeams { get; internal set; } = true;
    }
}