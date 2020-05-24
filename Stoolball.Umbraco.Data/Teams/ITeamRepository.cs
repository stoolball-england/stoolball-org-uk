using Stoolball.Teams;
using System;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Teams
{
    public interface ITeamRepository
    {
        /// <summary>
        /// Creates a stoolball team and populates the <see cref="Team.TeamId"/>
        /// </summary>
        /// <returns>The created team</returns>
        Task<Team> CreateTeam(Team team, Guid memberKey, string memberName);

        /// <summary>
        /// Updates a stoolball team
        /// </summary>
        Task<Team> UpdateTeam(Team team, Guid memberKey, string memberName);

        /// <summary>
        /// Updates a transient stoolball team
        /// </summary>
        Task<Team> UpdateTransientTeam(Team team, Guid memberKey, string memberName);
    }
}