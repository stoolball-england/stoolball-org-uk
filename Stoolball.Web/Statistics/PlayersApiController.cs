using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Humanizer;
using Stoolball.Statistics;
using Stoolball.Web.WebApi;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Mapping;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.WebApi;

namespace Stoolball.Web.Statistics
{
    public class PlayersApiController : UmbracoApiController
    {
        private readonly IPlayerDataSource _playerDataSource;

        public PlayersApiController(IGlobalSettings globalSettings, IUmbracoContextAccessor umbracoContextAccessor, ISqlContext sqlContext, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IRuntimeState runtimeState, UmbracoHelper umbracoHelper, UmbracoMapper umbracoMapper, IPlayerDataSource playerDataSource) :

            base(globalSettings, umbracoContextAccessor, sqlContext, serviceContext, appCaches, profilingLogger, runtimeState, umbracoHelper, umbracoMapper)
        {
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
        }

        [HttpGet]
        [Route("api/players/autocomplete")]
        public async Task<AutocompleteResultSet> Autocomplete([FromUri] string query, [FromUri] string[] teams)
        {
            if (teams is null)
            {
                throw new ArgumentNullException(nameof(teams));
            }

            var playerQuery = new PlayerIdentityFilter { Query = query };
            foreach (var guid in teams)
            {
                if (guid == null) continue;

                try
                {
                    playerQuery.TeamIds.Add(new Guid(guid));
                }
                catch (FormatException)
                {
                    // ignore that one
                }
            }
            var players = await _playerDataSource.ReadPlayerIdentities(playerQuery).ConfigureAwait(false);
            return new AutocompleteResultSet
            {
                suggestions = players.Select(x => new AutocompleteResult
                {
                    value = x.PlayerIdentityName,
                    data = new
                    {
                        playerIdentityId = x.PlayerIdentityId.ToString(),
                        playerIdentityName = x.PlayerIdentityName,
                        playerRecord = BuildPlayerRecord(x, playerQuery.TeamIds.Count != 1),
                        teamId = x.Team.TeamId,
                        teamName = x.Team.TeamName
                    }
                })
            };
        }

        private static string BuildPlayerRecord(PlayerIdentity playerIdentity, bool showTeam)
        {
            var value = new StringBuilder("match".ToQuantity(playerIdentity.TotalMatches.HasValue ? playerIdentity.TotalMatches.Value : 0));
            if (showTeam)
            {
                value.Append(" for ").Append(playerIdentity.Team.TeamName);

            }
            if (playerIdentity.FirstPlayed.HasValue)
            {
                var firstYear = playerIdentity.FirstPlayed.Value.Year;
                var lastYear = playerIdentity.LastPlayed.Value.Year;
                value.Append(' ').Append(firstYear);
                if (firstYear != lastYear)
                {
                    value.Append('–').Append(lastYear.ToString(CultureInfo.InvariantCulture).Substring(2));
                }
            }
            return value.ToString();
        }
    }
}