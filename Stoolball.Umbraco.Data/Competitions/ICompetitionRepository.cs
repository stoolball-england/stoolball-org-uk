using Stoolball.Competitions;
using System;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Competitions
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
    }
}