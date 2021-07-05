using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Umbraco.Core;
using Umbraco.Web;
using Umbraco.Web.Routing;

namespace Stoolball.Web.Routing
{
    /// <summary>
    /// Looks for routes that correspond to stoolball entities, and directs them to an 
    /// instance of the 'Stoolball router' document type where it's handled by <see cref="StoolballRouterController"/>.
    /// </summary>
    public class StoolballRouteContentFinder : IContentFinder
    {
        public bool TryFindContent(PublishedRequest request)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var matchedRouteType = MatchStoolballRouteType(request.Uri);
            if (matchedRouteType.HasValue)
            {
                // Direct the response to the 'Stoolball router' document type to be handled by StoolballRouterController
                var router = request.UmbracoContext.Content.GetSingleByXPath("//stoolballRouter");

                if (router != null)
                {
                    request.PublishedContent = router;
                    request.TrySetTemplate(matchedRouteType.Value.ToString());
                    return request.HasTemplate && router.IsAllowedTemplate(request.TemplateAlias);
                }
            }

            return false;
        }

        /// <summary>
        /// Matches a request URL to a route reserved for stoolball entities
        /// </summary>
        /// <param name="requestUrl">The request URL to test</param>
        /// <returns>The stoolball route type matching the URL, or <c>null</c> for no match</returns>
        internal static StoolballRouteType? MatchStoolballRouteType(Uri requestUrl)
        {
            var path = requestUrl.GetAbsolutePathDecoded();
            const string ANY_VALID_ROUTE = "[a-z0-9-]+";
            const string SLASH = @"\/";
            const string OPTIONAL_SLASH = @"\/?";
            const string SEASON = @"[0-9]{4}(-[0-9]{2})?";
            const string INNINGS = @"[0-9]+";

            var routeTypes = new Dictionary<string, StoolballRouteType>
            {
                // Match /prefix or /prefix/ but not /prefix/invalid, in upper, lower or mixed case
                { $"competitions{OPTIONAL_SLASH}", StoolballRouteType.Competitions },
                { $"teams{OPTIONAL_SLASH}", StoolballRouteType.Teams },
                { $"matches{OPTIONAL_SLASH}", StoolballRouteType.Matches },
                { $"tournaments{OPTIONAL_SLASH}", StoolballRouteType.Tournaments },
                { $"locations{OPTIONAL_SLASH}", StoolballRouteType.MatchLocations },

                // Match /matches/rss, /matches/ics, /tournaments/ics
                // /tournaments/all/rss, /tournaments/ladies/rss, /tournaments/mixed/rss, /tournaments/junior/rss 
                // /tournaments/all/calendar/ics, /tournaments/ladies/calendar/ics, /tournaments/mixed/calendar/ics, /tournaments/junior/calendar/ics
                // but nothing else, in upper, lower or mixed case
                // Important for these to be before the rules that match individual matches and tournaments.
                { $"tournaments{SLASH}(all|ladies|mixed|junior){SLASH}rss{OPTIONAL_SLASH}", StoolballRouteType.TournamentsRss }, // old site
                { $"tournaments{SLASH}(all|ladies|mixed|junior){SLASH}calendar{SLASH}ics{OPTIONAL_SLASH}", StoolballRouteType.MatchesCalendar }, // old site
                { $"tournaments{SLASH}rss{OPTIONAL_SLASH}", StoolballRouteType.TournamentsRss }, // preferred URL
                { $"matches{SLASH}rss{OPTIONAL_SLASH}", StoolballRouteType.MatchesRss },
                { $"(matches|tournaments){SLASH}ics{OPTIONAL_SLASH}", StoolballRouteType.MatchesCalendar },
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}rss{OPTIONAL_SLASH}", StoolballRouteType.MatchesRss },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}rss{OPTIONAL_SLASH}", StoolballRouteType.MatchesRss },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}rss{OPTIONAL_SLASH}", StoolballRouteType.MatchesRss },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}rss{OPTIONAL_SLASH}", StoolballRouteType.MatchesRss },

                // Match /prefix/example-entity or /prefix/action, but not /prefix, /prefix/, or /prefix/example-entity/invalid, 
                // in upper, lower or mixed case
                { $"clubs{SLASH}add{OPTIONAL_SLASH}", StoolballRouteType.CreateClub },
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.Club },
                { $"teams{SLASH}add{OPTIONAL_SLASH}", StoolballRouteType.CreateTeam },
                { $"teams{SLASH}map{OPTIONAL_SLASH}", StoolballRouteType.TeamsMap },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.Team},
                { $"locations{SLASH}add{OPTIONAL_SLASH}", StoolballRouteType.CreateMatchLocation},
                { $"locations{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.MatchLocation},
                { $"competitions{SLASH}add{OPTIONAL_SLASH}", StoolballRouteType.CreateCompetition },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.Competition },
                { $"matches{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.Match },
                { $"matches{SLASH}{ANY_VALID_ROUTE}{SLASH}ics{OPTIONAL_SLASH}", StoolballRouteType.MatchesCalendar },
                { $"tournaments{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.Tournament },
                { $"tournaments{SLASH}{ANY_VALID_ROUTE}{SLASH}ics{OPTIONAL_SLASH}", StoolballRouteType.MatchesCalendar },
                { $"players{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.Player },
                { $"players{SLASH}{ANY_VALID_ROUTE}{SLASH}bowling{OPTIONAL_SLASH}", StoolballRouteType.PlayerBowling },
                { $"players{SLASH}{ANY_VALID_ROUTE}{SLASH}fielding{OPTIONAL_SLASH}", StoolballRouteType.PlayerFielding },
                { $"players{SLASH}{ANY_VALID_ROUTE}{SLASH}individual-scores{OPTIONAL_SLASH}", StoolballRouteType.IndividualScores },
                { $"players{SLASH}{ANY_VALID_ROUTE}{SLASH}bowling-figures{OPTIONAL_SLASH}", StoolballRouteType.BowlingFigures },
                { $"players{SLASH}{ANY_VALID_ROUTE}{SLASH}catches{OPTIONAL_SLASH}", StoolballRouteType.Catches },
                { $"players{SLASH}{ANY_VALID_ROUTE}{SLASH}run-outs{OPTIONAL_SLASH}", StoolballRouteType.RunOuts },

                // Match /competitions/example-entity/2020, /competitions/example-entity/2020-21, 
                // but not /competitions, /competitions/, /competitions/example-entity, /competitions/example-entity/invalid 
                // or /competitions/example-entity/2020/invalid, in upper, lower or mixed case
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}[0-9]{{4}}{OPTIONAL_SLASH}(-[0-9]{{2}}{OPTIONAL_SLASH})?", StoolballRouteType.Season },
                
                // Match /competitions/example-entity/2020/matches, /competitions/example-entity/2020-21/matches/, 
                // but not /competitions, /competitions/, /competitions/example-entity/2020, /competitions/example-entity/invalid 
                // or /competitions/example-entity/2020/invalid, in upper, lower or mixed case
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}matches{OPTIONAL_SLASH}", StoolballRouteType.MatchesForSeason },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}statistics{OPTIONAL_SLASH}", StoolballRouteType.SeasonStatistics },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}statistics{SLASH}individual-scores{OPTIONAL_SLASH}", StoolballRouteType.IndividualScores },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}statistics{SLASH}most-runs{OPTIONAL_SLASH}", StoolballRouteType.MostRuns },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}statistics{SLASH}most-wickets{OPTIONAL_SLASH}", StoolballRouteType.MostWickets },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}statistics{SLASH}most-catches{OPTIONAL_SLASH}", StoolballRouteType.MostCatches },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}statistics{SLASH}most-run-outs{OPTIONAL_SLASH}", StoolballRouteType.MostRunOuts },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}statistics{SLASH}batting-average{OPTIONAL_SLASH}", StoolballRouteType.BattingAverage },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}statistics{SLASH}bowling-average{OPTIONAL_SLASH}", StoolballRouteType.BowlingAverage },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}statistics{SLASH}economy-rate{OPTIONAL_SLASH}", StoolballRouteType.EconomyRate },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}statistics{SLASH}bowling-figures{OPTIONAL_SLASH}", StoolballRouteType.BowlingFigures },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}matches{SLASH}add{SLASH}training{OPTIONAL_SLASH}", StoolballRouteType.CreateTrainingSession },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}matches{SLASH}add{SLASH}friendly{OPTIONAL_SLASH}", StoolballRouteType.CreateFriendlyMatch },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}matches{SLASH}add{SLASH}knockout{OPTIONAL_SLASH}", StoolballRouteType.CreateKnockoutMatch },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}matches{SLASH}add{SLASH}league{OPTIONAL_SLASH}", StoolballRouteType.CreateLeagueMatch },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}matches{SLASH}add{SLASH}tournament{OPTIONAL_SLASH}", StoolballRouteType.CreateTournament },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}map{OPTIONAL_SLASH}", StoolballRouteType.SeasonMap },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}table{OPTIONAL_SLASH}", StoolballRouteType.SeasonResultsTable },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.SeasonActions },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}edit{SLASH}season{OPTIONAL_SLASH}", StoolballRouteType.EditSeason },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}edit{SLASH}table{OPTIONAL_SLASH}", StoolballRouteType.EditSeasonResultsTable },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}edit{SLASH}teams{OPTIONAL_SLASH}", StoolballRouteType.EditSeasonTeams },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}{SEASON}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteSeason },

                // Match /teams/example-team/valid or /teams/example-team/valid/ but not /teams, /teams/
                // /teams/example-team, /teams/example-team/ or /teams/example-team/invalid in upper, lower or mixed case
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{OPTIONAL_SLASH}", StoolballRouteType.MatchesForClub },
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}ics{OPTIONAL_SLASH}", StoolballRouteType.MatchesCalendar },
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{OPTIONAL_SLASH}", StoolballRouteType.ClubStatistics },
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}individual-scores{OPTIONAL_SLASH}", StoolballRouteType.IndividualScores},
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-runs{OPTIONAL_SLASH}", StoolballRouteType.MostRuns},
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-wickets{OPTIONAL_SLASH}", StoolballRouteType.MostWickets},
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-catches{OPTIONAL_SLASH}", StoolballRouteType.MostCatches },
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-run-outs{OPTIONAL_SLASH}", StoolballRouteType.MostRunOuts },
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}batting-average{OPTIONAL_SLASH}", StoolballRouteType.BattingAverage },
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}bowling-average{OPTIONAL_SLASH}", StoolballRouteType.BowlingAverage },
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}economy-rate{OPTIONAL_SLASH}", StoolballRouteType.EconomyRate },
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}bowling-figures{OPTIONAL_SLASH}", StoolballRouteType.BowlingFigures},
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{OPTIONAL_SLASH}", StoolballRouteType.TeamStatistics},
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}individual-scores{OPTIONAL_SLASH}", StoolballRouteType.IndividualScores},
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-runs{OPTIONAL_SLASH}", StoolballRouteType.MostRuns},
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-wickets{OPTIONAL_SLASH}", StoolballRouteType.MostWickets},
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-catches{OPTIONAL_SLASH}", StoolballRouteType.MostCatches },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-run-outs{OPTIONAL_SLASH}", StoolballRouteType.MostRunOuts },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}batting-average{OPTIONAL_SLASH}", StoolballRouteType.BattingAverage },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}bowling-average{OPTIONAL_SLASH}", StoolballRouteType.BowlingAverage },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}economy-rate{OPTIONAL_SLASH}", StoolballRouteType.EconomyRate },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}bowling-figures{OPTIONAL_SLASH}", StoolballRouteType.BowlingFigures},
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.ClubActions },
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}club{OPTIONAL_SLASH}", StoolballRouteType.EditClub },
                { $"clubs{SLASH}{ANY_VALID_ROUTE}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteClub },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}players{OPTIONAL_SLASH}", StoolballRouteType.PlayersForTeam },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{OPTIONAL_SLASH}", StoolballRouteType.MatchesForTeam },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}ics{OPTIONAL_SLASH}", StoolballRouteType.MatchesCalendar },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}add{SLASH}training{OPTIONAL_SLASH}", StoolballRouteType.CreateTrainingSession },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}add{SLASH}friendly{OPTIONAL_SLASH}", StoolballRouteType.CreateFriendlyMatch },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}add{SLASH}knockout{OPTIONAL_SLASH}", StoolballRouteType.CreateKnockoutMatch },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}add{SLASH}league{OPTIONAL_SLASH}", StoolballRouteType.CreateLeagueMatch },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}add{SLASH}tournament{OPTIONAL_SLASH}", StoolballRouteType.CreateTournament },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.TeamActions },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}team{OPTIONAL_SLASH}", StoolballRouteType.EditTeam },
                { $"teams{SLASH}{ANY_VALID_ROUTE}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteTeam },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{OPTIONAL_SLASH}", StoolballRouteType.MatchesForMatchLocation },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}ics{OPTIONAL_SLASH}", StoolballRouteType.MatchesCalendar },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{OPTIONAL_SLASH}", StoolballRouteType.MatchLocationStatistics },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}individual-scores{OPTIONAL_SLASH}", StoolballRouteType.IndividualScores },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-runs{OPTIONAL_SLASH}", StoolballRouteType.MostRuns },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-wickets{OPTIONAL_SLASH}", StoolballRouteType.MostWickets },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-catches{OPTIONAL_SLASH}", StoolballRouteType.MostCatches },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-run-outs{OPTIONAL_SLASH}", StoolballRouteType.MostRunOuts },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}batting-average{OPTIONAL_SLASH}", StoolballRouteType.BattingAverage },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}bowling-average{OPTIONAL_SLASH}", StoolballRouteType.BowlingAverage },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}economy-rate{OPTIONAL_SLASH}" , StoolballRouteType.EconomyRate },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}bowling-figures{OPTIONAL_SLASH}", StoolballRouteType.BowlingFigures },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.MatchLocationActions },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}location{OPTIONAL_SLASH}", StoolballRouteType.EditMatchLocation },
                { $"locations{SLASH}{ANY_VALID_ROUTE}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteMatchLocation },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}matches{SLASH}ics{OPTIONAL_SLASH}", StoolballRouteType.MatchesCalendar },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{OPTIONAL_SLASH}", StoolballRouteType.CompetitionStatistics },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}individual-scores{OPTIONAL_SLASH}", StoolballRouteType.IndividualScores },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-runs{OPTIONAL_SLASH}", StoolballRouteType.MostRuns },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-wickets{OPTIONAL_SLASH}", StoolballRouteType.MostWickets },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-catches{OPTIONAL_SLASH}", StoolballRouteType.MostCatches },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}most-run-outs{OPTIONAL_SLASH}", StoolballRouteType.MostRunOuts },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}batting-average{OPTIONAL_SLASH}" , StoolballRouteType.BattingAverage },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}bowling-average{OPTIONAL_SLASH}" , StoolballRouteType.BowlingAverage },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}economy-rate{OPTIONAL_SLASH}" , StoolballRouteType.EconomyRate },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}statistics{SLASH}bowling-figures{OPTIONAL_SLASH}", StoolballRouteType.BowlingFigures },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}add{OPTIONAL_SLASH}", StoolballRouteType.CreateSeason },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.CompetitionActions },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}competition{OPTIONAL_SLASH}", StoolballRouteType.EditCompetition },
                { $"competitions{SLASH}{ANY_VALID_ROUTE}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteCompetition },
                { $"matches{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.MatchActions },
                { $"matches{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}friendly{OPTIONAL_SLASH}", StoolballRouteType.EditFriendlyMatch },
                { $"matches{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}league{OPTIONAL_SLASH}", StoolballRouteType.EditLeagueMatch },
                { $"matches{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}knockout{OPTIONAL_SLASH}", StoolballRouteType.EditKnockoutMatch },
                { $"matches{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}training{OPTIONAL_SLASH}", StoolballRouteType.EditTrainingSession },
                { $"matches{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}start-of-play{OPTIONAL_SLASH}", StoolballRouteType.EditStartOfPlay },
                { $"matches{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}innings{SLASH}{INNINGS}{SLASH}batting{OPTIONAL_SLASH}", StoolballRouteType.EditBattingScorecard },
                { $"matches{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}innings{SLASH}{INNINGS}{SLASH}bowling{OPTIONAL_SLASH}", StoolballRouteType.EditBowlingScorecard },
                { $"matches{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}close-of-play{OPTIONAL_SLASH}", StoolballRouteType.EditCloseOfPlay },
                { $"matches{SLASH}{ANY_VALID_ROUTE}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteMatch },
                { $"tournaments{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.TournamentActions },
                { $"tournaments{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}tournament{OPTIONAL_SLASH}", StoolballRouteType.EditTournament },
                { $"tournaments{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}matches{OPTIONAL_SLASH}", StoolballRouteType.EditTournamentMatches },
                { $"tournaments{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}teams{OPTIONAL_SLASH}", StoolballRouteType.EditTournamentTeams },
                { $"tournaments{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{SLASH}seasons{OPTIONAL_SLASH}", StoolballRouteType.EditTournamentSeasons },
                { $"tournaments{SLASH}{ANY_VALID_ROUTE}{SLASH}delete{OPTIONAL_SLASH}", StoolballRouteType.DeleteTournament },

                // Match /tournaments/example123/teams/example-team, /tournaments/example123/teams/example-team/ or /tournaments/example123/teams/example-team/edit 
                // but not /tournaments/example123, /tournaments/example123/, /tournaments/example123/teams, /tournaments/example123/teams/,
                // /tournaments/example123/invalid or /tournaments/example123/teams/example-team/invalid in upper, lower or mixed case
                { $"tournaments{SLASH}{ANY_VALID_ROUTE}{SLASH}teams{SLASH}{ANY_VALID_ROUTE}{OPTIONAL_SLASH}", StoolballRouteType.TransientTeam },
                { $"tournaments{SLASH}{ANY_VALID_ROUTE}{SLASH}teams{SLASH}{ANY_VALID_ROUTE}{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.EditTransientTeam },

                // Match /play/statistics, /play/statistics/edit or /play/statistics/example-statistic
                { $"play{SLASH}statistics{OPTIONAL_SLASH}", StoolballRouteType.Statistics },
                { $"play{SLASH}statistics{SLASH}edit{OPTIONAL_SLASH}", StoolballRouteType.EditStatistics },
                { $"play{SLASH}statistics{SLASH}individual-scores{OPTIONAL_SLASH}", StoolballRouteType.IndividualScores },
                { $"play{SLASH}statistics{SLASH}bowling-figures{OPTIONAL_SLASH}", StoolballRouteType.BowlingFigures },
                { $"play{SLASH}statistics{SLASH}most-runs{OPTIONAL_SLASH}", StoolballRouteType.MostRuns },
                { $"play{SLASH}statistics{SLASH}most-wickets{OPTIONAL_SLASH}", StoolballRouteType.MostWickets },
                { $"play{SLASH}statistics{SLASH}most-catches{OPTIONAL_SLASH}", StoolballRouteType.MostCatches },
                { $"play{SLASH}statistics{SLASH}most-run-outs{OPTIONAL_SLASH}", StoolballRouteType.MostRunOuts },
                { $"play{SLASH}statistics{SLASH}batting-average{OPTIONAL_SLASH}", StoolballRouteType.BattingAverage },
                { $"play{SLASH}statistics{SLASH}bowling-average{OPTIONAL_SLASH}", StoolballRouteType.BowlingAverage },
                { $"play{SLASH}statistics{SLASH}economy-rate{OPTIONAL_SLASH}", StoolballRouteType.EconomyRate }
            };

            foreach (var routePattern in routeTypes.Keys)
            {
                if (Regex.IsMatch(path, $@"^{SLASH}{routePattern}$", RegexOptions.IgnoreCase))
                {
                    return routeTypes[routePattern];
                }
            }

            return null;
        }
    }
}