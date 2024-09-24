using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Statistics;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.WebApi
{
    public class PlayersApiController : UmbracoApiController
    {
        private readonly IPlayerDataSource _playerDataSource;

        public PlayersApiController(IPlayerDataSource playerDataSource) : base()
        {
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
        }

        [HttpGet]
        [Route("api/players/autocomplete")]
        public async Task<AutocompleteResultSet> Autocomplete(
            [FromQuery] string query,
            [FromQuery] string[] not,
            [FromQuery] string[] teams,
            [FromQuery] bool includeLinkedToMember = true,
            [FromQuery] bool includeMultipleIdentities = true)
        {
            if (not is null)
            {
                throw new ArgumentNullException(nameof(not));
            }

            if (teams is null)
            {
                throw new ArgumentNullException(nameof(teams));
            }

            var playerFilter = new PlayerFilter { Query = query };
            playerFilter.IncludePlayersAndIdentitiesLinkedToAMember = includeLinkedToMember;
            playerFilter.IncludePlayersAndIdentitiesWithMultipleIdentities = includeMultipleIdentities;

            foreach (var guid in not)
            {
                if (guid == null) continue;

                try
                {
                    playerFilter.ExcludePlayerIdentityIds.Add(new Guid(guid));
                }
                catch (FormatException)
                {
                    // ignore that one
                }
            }

            foreach (var guid in teams)
            {
                if (guid == null) continue;

                try
                {
                    playerFilter.TeamIds.Add(new Guid(guid));
                }
                catch (FormatException)
                {
                    // ignore that one
                }
            }
            var players = await _playerDataSource.ReadPlayerIdentities(playerFilter).ConfigureAwait(false);
            return new AutocompleteResultSet
            {
                suggestions = players.Select(x => new AutocompleteResult
                {
                    value = x.PlayerIdentityName,
                    data = new
                    {
                        playerIdentityId = x.PlayerIdentityId.ToString(),
                        playerIdentityName = x.PlayerIdentityName,
                        playerRecord = BuildPlayerRecord(x, playerFilter.TeamIds.Count != 1),
                        teamId = x.Team!.TeamId,
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
                value.Append(" for ").Append(playerIdentity.Team?.TeamName);

            }
            if (playerIdentity.FirstPlayed.HasValue)
            {
                var firstYear = playerIdentity.FirstPlayed.Value.Year;
                var lastYear = playerIdentity.LastPlayed.HasValue ? playerIdentity.LastPlayed.Value.Year : (int?)null;
                value.Append(' ').Append(firstYear);
                if (firstYear != lastYear)
                {
                    value.Append('–').Append(lastYear?.ToString(CultureInfo.InvariantCulture).Substring(2));
                }
            }
            return value.ToString();
        }
    }
}