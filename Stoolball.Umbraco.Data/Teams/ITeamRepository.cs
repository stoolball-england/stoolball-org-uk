using Stoolball.Teams;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Teams
{
    public interface ITeamRepository
    {
        /// <summary>
        /// Creates a stoolball team and populates the <see cref="Team.TeamId"/>
        /// </summary>
        /// <returns>The created team</returns>
        Task<Team> CreateTeam(Team team, Guid memberKey, string memberUsername, string memberName);

        /// <summary>
        /// Creates a team using an existing transaction
        /// </summary>
        Task<Team> CreateTeam(Team team, IDbTransaction transaction, string memberUsername);

        /// <summary>
        /// Updates a stoolball team
        /// </summary>
        Task<Team> UpdateTeam(Team team, Guid memberKey, string memberName);

        /// <summary>
        /// Updates a transient stoolball team
        /// </summary>
        Task<Team> UpdateTransientTeam(Team team, Guid memberKey, string memberName);

        /// <summary>
        /// Delete a stoolball team
        /// </summary>
        Task DeleteTeam(Team team, Guid memberKey, string memberName);
    }
}