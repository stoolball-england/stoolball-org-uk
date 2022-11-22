using System;
using System.Threading.Tasks;
using Stoolball.Competitions;

namespace Stoolball.Data.Abstractions
{
    public interface ICompetitionRepository
    {
        /// <summary>
        /// Creates a stoolball competition and populates the <see cref="Competition.CompetitionId"/>
        /// </summary>
        /// <returns>The created competition</returns>
        Task<Competition> CreateCompetition(Competition competition, Guid memberKey, string memberName);

        /// <summary>
        /// Updates a stoolball competition
        /// </summary>
        Task<Competition> UpdateCompetition(Competition competition, Guid memberKey, string memberName);

        /// <summary>
        /// Delete a stoolball competition
        /// </summary>
        Task DeleteCompetition(Competition competition, Guid memberKey, string memberName);
    }
}