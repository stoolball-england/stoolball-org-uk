﻿using System;
using Stoolball.Web.Routing;
using Xunit;

namespace Stoolball.Web.UnitTests.Routing
{
    public class StoolballRouteParserTests
    {
        [Theory]
        [InlineData("https://example.org/teams", StoolballRouteType.Teams)]
        [InlineData("https://example.org/teams/add", StoolballRouteType.CreateTeam)]
        [InlineData("https://example.org/teams/map", StoolballRouteType.TeamsMap)]
        [InlineData("https://example.org/teams/example123", StoolballRouteType.Team)]
        [InlineData("https://example.org/teams/example123/edit", StoolballRouteType.TeamActions)]
        [InlineData("https://example.org/teams/example123/edit/team", StoolballRouteType.EditTeam)]
        [InlineData("https://example.org/teams/example123/edit/players", StoolballRouteType.EditPlayersForTeam)]
        [InlineData("https://example.org/teams/example123/edit/players/some-player", StoolballRouteType.PlayerIdentityActions)]
        [InlineData("https://example.org/teams/example123/edit/players/some-player/rename", StoolballRouteType.RenamePlayerIdentity)]
        [InlineData("https://example.org/teams/example123/edit/players/some-player/statistics", StoolballRouteType.LinkedPlayersForIdentity)]
        [InlineData("https://example.org/teams/example123/delete", StoolballRouteType.DeleteTeam)]
        [InlineData("https://example.org/teams/example123/players", StoolballRouteType.PlayersForTeam)]
        [InlineData("https://example.org/teams/example123/matches", StoolballRouteType.MatchesForTeam)]
        [InlineData("https://example.org/teams/example123/matches/rss", StoolballRouteType.MatchesRss)]
        [InlineData("https://example.org/teams/example123/matches/ics", StoolballRouteType.MatchesCalendar)]
        [InlineData("https://example.org/teams/example123/statistics", StoolballRouteType.TeamStatistics)]
        [InlineData("https://example.org/teams/example123/statistics/individual-scores", StoolballRouteType.IndividualScores)]
        [InlineData("https://example.org/teams/example123/statistics/most-scores-of-50", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/teams/example123/statistics/most-scores-of-100", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/teams/example123/statistics/bowling-figures", StoolballRouteType.BowlingFigures)]
        [InlineData("https://example.org/teams/example123/statistics/player-performances", StoolballRouteType.PlayerPerformances)]
        [InlineData("https://example.org/teams/example123/statistics/player-of-match", StoolballRouteType.PlayerOfTheMatchAwards)]
        [InlineData("https://example.org/teams/example123/statistics/most-player-of-match", StoolballRouteType.MostPlayerOfTheMatchAwards)]
        [InlineData("https://example.org/teams/example123/statistics/most-runs", StoolballRouteType.MostRuns)]
        [InlineData("https://example.org/teams/example123/statistics/most-wickets", StoolballRouteType.MostWickets)]
        [InlineData("https://example.org/teams/example123/statistics/most-5-wickets", StoolballRouteType.MostXWickets)]
        [InlineData("https://example.org/teams/example123/statistics/most-catches", StoolballRouteType.MostCatches)]
        [InlineData("https://example.org/teams/example123/statistics/most-run-outs", StoolballRouteType.MostRunOuts)]
        [InlineData("https://example.org/teams/example123/statistics/batting-average", StoolballRouteType.BattingAverage)]
        [InlineData("https://example.org/teams/example123/statistics/batting-strike-rate", StoolballRouteType.BattingStrikeRate)]
        [InlineData("https://example.org/teams/example123/statistics/bowling-strike-rate", StoolballRouteType.BowlingStrikeRate)]
        [InlineData("https://example.org/teams/example123/statistics/bowling-average", StoolballRouteType.BowlingAverage)]
        [InlineData("https://example.org/teams/example123/statistics/economy-rate", StoolballRouteType.EconomyRate)]
        [InlineData("https://example.org/teams/example123/matches/add/training", StoolballRouteType.CreateTrainingSession)]
        [InlineData("https://example.org/teams/example123/matches/add/friendly", StoolballRouteType.CreateFriendlyMatch)]
        [InlineData("https://example.org/teams/example123/matches/add/knockout", StoolballRouteType.CreateKnockoutMatch)]
        [InlineData("https://example.org/teams/example123/matches/add/league", StoolballRouteType.CreateLeagueMatch)]
        [InlineData("https://example.org/teams/example123/matches/add/tournament", StoolballRouteType.CreateTournament)]
        [InlineData("https://example.org/tournaments/example123/teams/example-team", StoolballRouteType.TransientTeam)]
        [InlineData("https://example.org/tournaments/example123/teams/example-team/edit", StoolballRouteType.EditTransientTeam)]
        [InlineData("https://example.org/locations", StoolballRouteType.MatchLocations)]
        [InlineData("https://example.org/locations/add", StoolballRouteType.CreateMatchLocation)]
        [InlineData("https://example.org/locations/example-location/", StoolballRouteType.MatchLocation)]
        [InlineData("https://example.org/locations/example-location/edit", StoolballRouteType.MatchLocationActions)]
        [InlineData("https://example.org/locations/example-location/edit/location", StoolballRouteType.EditMatchLocation)]
        [InlineData("https://example.org/locations/example-location/delete", StoolballRouteType.DeleteMatchLocation)]
        [InlineData("https://example.org/locations/example-location/matches", StoolballRouteType.MatchesForMatchLocation)]
        [InlineData("https://example.org/locations/example-location/matches/rss", StoolballRouteType.MatchesRss)]
        [InlineData("https://example.org/locations/example-location/matches/ics", StoolballRouteType.MatchesCalendar)]
        [InlineData("https://example.org/locations/example-location/statistics", StoolballRouteType.MatchLocationStatistics)]
        [InlineData("https://example.org/locations/example-location/statistics/individual-scores", StoolballRouteType.IndividualScores)]
        [InlineData("https://example.org/locations/example-location/statistics/most-scores-of-50", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/locations/example-location/statistics/most-scores-of-100", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/locations/example-location/statistics/most-runs", StoolballRouteType.MostRuns)]
        [InlineData("https://example.org/locations/example-location/statistics/most-wickets", StoolballRouteType.MostWickets)]
        [InlineData("https://example.org/locations/example-location/statistics/most-5-wickets", StoolballRouteType.MostXWickets)]
        [InlineData("https://example.org/locations/example-location/statistics/most-catches", StoolballRouteType.MostCatches)]
        [InlineData("https://example.org/locations/example-location/statistics/most-run-outs", StoolballRouteType.MostRunOuts)]
        [InlineData("https://example.org/locations/example-location/statistics/batting-average", StoolballRouteType.BattingAverage)]
        [InlineData("https://example.org/locations/example-location/statistics/batting-strike-rate", StoolballRouteType.BattingStrikeRate)]
        [InlineData("https://example.org/locations/example-location/statistics/bowling-strike-rate", StoolballRouteType.BowlingStrikeRate)]
        [InlineData("https://example.org/locations/example-location/statistics/bowling-average", StoolballRouteType.BowlingAverage)]
        [InlineData("https://example.org/locations/example-location/statistics/economy-rate", StoolballRouteType.EconomyRate)]
        [InlineData("https://example.org/locations/example-location/statistics/bowling-figures", StoolballRouteType.BowlingFigures)]
        [InlineData("https://example.org/locations/example123/statistics/player-performances", StoolballRouteType.PlayerPerformances)]
        [InlineData("https://example.org/locations/example123/statistics/player-of-match", StoolballRouteType.PlayerOfTheMatchAwards)]
        [InlineData("https://example.org/locations/example123/statistics/most-player-of-match", StoolballRouteType.MostPlayerOfTheMatchAwards)]
        [InlineData("https://example.org/clubs/add/", StoolballRouteType.CreateClub)]
        [InlineData("https://example.org/clubs/example-name/", StoolballRouteType.Club)]
        [InlineData("https://example.org/clubs/example-name/edit", StoolballRouteType.ClubActions)]
        [InlineData("https://example.org/clubs/example-name/edit/club", StoolballRouteType.EditClub)]
        [InlineData("https://example.org/clubs/example-name/delete", StoolballRouteType.DeleteClub)]
        [InlineData("https://example.org/clubs/example-club/matches", StoolballRouteType.MatchesForClub)]
        [InlineData("https://example.org/clubs/example-club/matches/ics", StoolballRouteType.MatchesCalendar)]
        [InlineData("https://example.org/clubs/example-club/matches/rss", StoolballRouteType.MatchesRss)]
        [InlineData("https://example.org/clubs/example-club/statistics", StoolballRouteType.ClubStatistics)]
        [InlineData("https://example.org/clubs/example-club/statistics/individual-scores", StoolballRouteType.IndividualScores)]
        [InlineData("https://example.org/clubs/example-club/statistics/most-scores-of-50", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/clubs/example-club/statistics/most-scores-of-100", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/clubs/example-club/statistics/bowling-figures", StoolballRouteType.BowlingFigures)]
        [InlineData("https://example.org/clubs/example123/statistics/player-performances", StoolballRouteType.PlayerPerformances)]
        [InlineData("https://example.org/clubs/example123/statistics/player-of-match", StoolballRouteType.PlayerOfTheMatchAwards)]
        [InlineData("https://example.org/clubs/example123/statistics/most-player-of-match", StoolballRouteType.MostPlayerOfTheMatchAwards)]
        [InlineData("https://example.org/clubs/example-club/statistics/most-runs", StoolballRouteType.MostRuns)]
        [InlineData("https://example.org/clubs/example-club/statistics/most-wickets", StoolballRouteType.MostWickets)]
        [InlineData("https://example.org/clubs/example-club/statistics/most-5-wickets", StoolballRouteType.MostXWickets)]
        [InlineData("https://example.org/clubs/example-club/statistics/most-catches", StoolballRouteType.MostCatches)]
        [InlineData("https://example.org/clubs/example-club/statistics/most-run-outs", StoolballRouteType.MostRunOuts)]
        [InlineData("https://example.org/clubs/example-club/statistics/batting-average", StoolballRouteType.BattingAverage)]
        [InlineData("https://example.org/clubs/example-club/statistics/batting-strike-rate", StoolballRouteType.BattingStrikeRate)]
        [InlineData("https://example.org/clubs/example-club/statistics/bowling-strike-rate", StoolballRouteType.BowlingStrikeRate)]
        [InlineData("https://example.org/clubs/example-club/statistics/bowling-average", StoolballRouteType.BowlingAverage)]
        [InlineData("https://example.org/clubs/example-club/statistics/economy-rate", StoolballRouteType.EconomyRate)]
        [InlineData("https://example.org/competitions", StoolballRouteType.Competitions)]
        [InlineData("https://example.org/competitions/add", StoolballRouteType.CreateCompetition)]
        [InlineData("https://example.org/competitions/example", StoolballRouteType.Competition)]
        [InlineData("https://example.org/competitions/example/matches/ics", StoolballRouteType.MatchesCalendar)]
        [InlineData("https://example.org/competitions/example/statistics", StoolballRouteType.CompetitionStatistics)]
        [InlineData("https://example.org/competitions/example/statistics/individual-scores", StoolballRouteType.IndividualScores)]
        [InlineData("https://example.org/competitions/example/statistics/most-scores-of-50", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/competitions/example/statistics/most-scores-of-100", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/competitions/example/statistics/most-runs", StoolballRouteType.MostRuns)]
        [InlineData("https://example.org/competitions/example/statistics/most-wickets", StoolballRouteType.MostWickets)]
        [InlineData("https://example.org/competitions/example/statistics/most-5-wickets", StoolballRouteType.MostXWickets)]
        [InlineData("https://example.org/competitions/example/statistics/most-catches", StoolballRouteType.MostCatches)]
        [InlineData("https://example.org/competitions/example/statistics/most-run-outs", StoolballRouteType.MostRunOuts)]
        [InlineData("https://example.org/competitions/example/statistics/batting-average", StoolballRouteType.BattingAverage)]
        [InlineData("https://example.org/competitions/example/statistics/batting-strike-rate", StoolballRouteType.BattingStrikeRate)]
        [InlineData("https://example.org/competitions/example/statistics/bowling-strike-rate/", StoolballRouteType.BowlingStrikeRate)]
        [InlineData("https://example.org/competitions/example/statistics/bowling-average", StoolballRouteType.BowlingAverage)]
        [InlineData("https://example.org/competitions/example/statistics/economy-rate", StoolballRouteType.EconomyRate)]
        [InlineData("https://example.org/competitions/example/statistics/bowling-figures", StoolballRouteType.BowlingFigures)]
        [InlineData("https://example.org/competitions/example/statistics/player-performances", StoolballRouteType.PlayerPerformances)]
        [InlineData("https://example.org/competitions/example/statistics/player-of-match", StoolballRouteType.PlayerOfTheMatchAwards)]
        [InlineData("https://example.org/competitions/example/statistics/most-player-of-match", StoolballRouteType.MostPlayerOfTheMatchAwards)]
        [InlineData("https://example.org/competitions/example/add", StoolballRouteType.CreateSeason)]
        [InlineData("https://example.org/competitions/example/edit/", StoolballRouteType.CompetitionActions)]
        [InlineData("https://example.org/competitions/example/edit/competition", StoolballRouteType.EditCompetition)]
        [InlineData("https://example.org/competitions/example/delete/", StoolballRouteType.DeleteCompetition)]
        [InlineData("https://example.org/competitions/example/matches/rss", StoolballRouteType.MatchesRss)]
        [InlineData("https://example.org/competitions/example/2020", StoolballRouteType.Season)]
        [InlineData("https://example.org/competitions/example/2020/statistics", StoolballRouteType.SeasonStatistics)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics", StoolballRouteType.SeasonStatistics)]
        [InlineData("https://example.org/competitions/example/2020/statistics/individual-scores", StoolballRouteType.IndividualScores)]
        [InlineData("https://example.org/competitions/example/2020/statistics/most-scores-of-50", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/competitions/example/2020/statistics/most-scores-of-100", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/individual-scores", StoolballRouteType.IndividualScores)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/most-scores-of-50", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/most-scores-of-100", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/competitions/example/2020/statistics/most-runs", StoolballRouteType.MostRuns)]
        [InlineData("https://example.org/competitions/example/2020/statistics/most-wickets", StoolballRouteType.MostWickets)]
        [InlineData("https://example.org/competitions/example/2020/statistics/most-5-wickets", StoolballRouteType.MostXWickets)]
        [InlineData("https://example.org/competitions/example/2020/statistics/most-catches", StoolballRouteType.MostCatches)]
        [InlineData("https://example.org/competitions/example/2020/statistics/most-run-outs", StoolballRouteType.MostRunOuts)]
        [InlineData("https://example.org/competitions/example/2020/statistics/batting-average", StoolballRouteType.BattingAverage)]
        [InlineData("https://example.org/competitions/example/2020/statistics/batting-strike-rate", StoolballRouteType.BattingStrikeRate)]
        [InlineData("https://example.org/competitions/example/2020/statistics/bowling-strike-rate", StoolballRouteType.BowlingStrikeRate)]
        [InlineData("https://example.org/competitions/example/2020/statistics/bowling-average", StoolballRouteType.BowlingAverage)]
        [InlineData("https://example.org/competitions/example/2020/statistics/economy-rate", StoolballRouteType.EconomyRate)]
        [InlineData("https://example.org/competitions/example/2020/statistics/bowling-figures", StoolballRouteType.BowlingFigures)]
        [InlineData("https://example.org/competitions/example/2020/statistics/player-performances", StoolballRouteType.PlayerPerformances)]
        [InlineData("https://example.org/competitions/example/2020/statistics/player-of-match", StoolballRouteType.PlayerOfTheMatchAwards)]
        [InlineData("https://example.org/competitions/example/2020/statistics/most-player-of-match", StoolballRouteType.MostPlayerOfTheMatchAwards)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/most-runs", StoolballRouteType.MostRuns)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/most-wickets", StoolballRouteType.MostWickets)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/most-5-wickets", StoolballRouteType.MostXWickets)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/most-catches", StoolballRouteType.MostCatches)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/most-run-outs", StoolballRouteType.MostRunOuts)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/batting-average", StoolballRouteType.BattingAverage)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/batting-strike-rate", StoolballRouteType.BattingStrikeRate)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/bowling-strike-rate", StoolballRouteType.BowlingStrikeRate)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/bowling-average", StoolballRouteType.BowlingAverage)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/economy-rate", StoolballRouteType.EconomyRate)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/bowling-figures", StoolballRouteType.BowlingFigures)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/player-performances", StoolballRouteType.PlayerPerformances)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/player-of-match", StoolballRouteType.PlayerOfTheMatchAwards)]
        [InlineData("https://example.org/competitions/example/2020-21/statistics/most-player-of-match", StoolballRouteType.MostPlayerOfTheMatchAwards)]
        [InlineData("https://example.org/competitions/example/2020/matches", StoolballRouteType.MatchesForSeason)]
        [InlineData("https://example.org/competitions/example/2020-21/matches", StoolballRouteType.MatchesForSeason)]
        [InlineData("https://example.org/competitions/example/2020/matches/add/training", StoolballRouteType.CreateTrainingSession)]
        [InlineData("https://example.org/competitions/example/2020/matches/add/friendly", StoolballRouteType.CreateFriendlyMatch)]
        [InlineData("https://example.org/competitions/example/2020/matches/add/league", StoolballRouteType.CreateLeagueMatch)]
        [InlineData("https://example.org/competitions/example/2020-21/matches/add/training", StoolballRouteType.CreateTrainingSession)]
        [InlineData("https://example.org/competitions/example/2020-21/matches/add/friendly", StoolballRouteType.CreateFriendlyMatch)]
        [InlineData("https://example.org/competitions/example/2020-21/matches/add/league", StoolballRouteType.CreateLeagueMatch)]
        [InlineData("https://example.org/competitions/example/2020/matches/add/knockout/", StoolballRouteType.CreateKnockoutMatch)]
        [InlineData("https://example.org/competitions/example/2020-21/matches/add/knockout", StoolballRouteType.CreateKnockoutMatch)]
        [InlineData("https://example.org/competitions/example/2020/map", StoolballRouteType.SeasonMap)]
        [InlineData("https://example.org/competitions/example/2020-21/map/", StoolballRouteType.SeasonMap)]
        [InlineData("https://example.org/competitions/example/2020/table", StoolballRouteType.SeasonResultsTable)]
        [InlineData("https://example.org/competitions/example/2020-21/table", StoolballRouteType.SeasonResultsTable)]
        [InlineData("https://example.org/competitions/example/2020/edit", StoolballRouteType.SeasonActions)]
        [InlineData("https://example.org/competitions/example/2020-21/edit", StoolballRouteType.SeasonActions)]
        [InlineData("https://example.org/competitions/example/2020/edit/season", StoolballRouteType.EditSeason)]
        [InlineData("https://example.org/competitions/example/2020-21/edit/season", StoolballRouteType.EditSeason)]
        [InlineData("https://example.org/competitions/example/2020/edit/table", StoolballRouteType.EditSeasonResultsTable)]
        [InlineData("https://example.org/competitions/example/2020-21/edit/table", StoolballRouteType.EditSeasonResultsTable)]
        [InlineData("https://example.org/competitions/example/2020/edit/teams", StoolballRouteType.EditSeasonTeams)]
        [InlineData("https://example.org/competitions/example/2020-21/edit/teams", StoolballRouteType.EditSeasonTeams)]
        [InlineData("https://example.org/competitions/example/2020/delete", StoolballRouteType.DeleteSeason)]
        [InlineData("https://example.org/competitions/example/2020-21/delete", StoolballRouteType.DeleteSeason)]
        [InlineData("https://example.org/matches", StoolballRouteType.Matches)]
        [InlineData("https://example.org/matches/example-match", StoolballRouteType.Match)]
        [InlineData("https://example.org/matches/example-match/ics", StoolballRouteType.MatchesCalendar)]
        [InlineData("https://example.org/matches/example-match/edit", StoolballRouteType.MatchActions)]
        [InlineData("https://example.org/matches/example-match/edit/friendly", StoolballRouteType.EditFriendlyMatch)]
        [InlineData("https://example.org/matches/example-match/edit/league", StoolballRouteType.EditLeagueMatch)]
        [InlineData("https://example.org/matches/example-match/edit/knockout", StoolballRouteType.EditKnockoutMatch)]
        [InlineData("https://example.org/matches/example-match/edit/format", StoolballRouteType.EditMatchFormat)]
        [InlineData("https://example.org/matches/example-match/edit/training", StoolballRouteType.EditTrainingSession)]
        [InlineData("https://example.org/matches/example-match/edit/start-of-play", StoolballRouteType.EditStartOfPlay)]
        [InlineData("https://example.org/matches/example-match/edit/innings/1/batting", StoolballRouteType.EditBattingScorecard)]
        [InlineData("https://example.org/matches/example-match/edit/innings/18/batting/", StoolballRouteType.EditBattingScorecard)]
        [InlineData("https://example.org/matches/example-match/edit/innings/1/bowling", StoolballRouteType.EditBowlingScorecard)]
        [InlineData("https://example.org/matches/example-match/edit/innings/18/bowling/", StoolballRouteType.EditBowlingScorecard)]
        [InlineData("https://example.org/matches/example-match/edit/close-of-play", StoolballRouteType.EditCloseOfPlay)]
        [InlineData("https://example.org/matches/example-match/delete", StoolballRouteType.DeleteMatch)]
        [InlineData("https://example.org/matches/rss", StoolballRouteType.MatchesRss)]
        [InlineData("https://example.org/matches/ics", StoolballRouteType.MatchesCalendar)]
        [InlineData("https://example.org/tournaments/ics", StoolballRouteType.MatchesCalendar)]
        [InlineData("https://example.org/tournaments/junior/calendar/ics", StoolballRouteType.MatchesCalendar)]
        [InlineData("https://example.org/tournaments", StoolballRouteType.Tournaments)]
        [InlineData("https://example.org/tournaments/rss", StoolballRouteType.TournamentsRss)]
        [InlineData("https://example.org/tournaments/all/rss", StoolballRouteType.TournamentsRss)]
        [InlineData("https://example.org/tournaments/example-tournament/", StoolballRouteType.Tournament)]
        [InlineData("https://example.org/tournaments/123-tournament/", StoolballRouteType.Tournament)]
        [InlineData("https://example.org/tournaments/123-tournament/ics", StoolballRouteType.MatchesCalendar)]
        [InlineData("https://example.org/tournaments/example-tournament/edit", StoolballRouteType.TournamentActions)]
        [InlineData("https://example.org/tournaments/123-tournament/edit/", StoolballRouteType.TournamentActions)]
        [InlineData("https://example.org/tournaments/example-tournament/edit/tournament", StoolballRouteType.EditTournament)]
        [InlineData("https://example.org/tournaments/123-tournament/edit/tournament/", StoolballRouteType.EditTournament)]
        [InlineData("https://example.org/tournaments/example-tournament/edit/matches", StoolballRouteType.EditTournamentMatches)]
        [InlineData("https://example.org/tournaments/123-tournament/edit/matches/", StoolballRouteType.EditTournamentMatches)]
        [InlineData("https://example.org/tournaments/example-tournament/edit/teams", StoolballRouteType.EditTournamentTeams)]
        [InlineData("https://example.org/tournaments/123-tournament/edit/teams/", StoolballRouteType.EditTournamentTeams)]
        [InlineData("https://example.org/tournaments/example-tournament/edit/seasons", StoolballRouteType.EditTournamentSeasons)]
        [InlineData("https://example.org/tournaments/123-tournament/edit/seasons/", StoolballRouteType.EditTournamentSeasons)]
        [InlineData("https://example.org/tournaments/example-tournament/delete", StoolballRouteType.DeleteTournament)]
        [InlineData("https://example.org/tournaments/123-tournament/delete/", StoolballRouteType.DeleteTournament)]
        [InlineData("https://example.org/play/statistics", StoolballRouteType.Statistics)]
        [InlineData("https://example.org/play/statistics/edit", StoolballRouteType.EditStatistics)]
        [InlineData("https://example.org/play/statistics/individual-scores", StoolballRouteType.IndividualScores)]
        [InlineData("https://example.org/play/statistics/most-scores-of-50", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/play/statistics/most-scores-of-100", StoolballRouteType.MostScoresOfX)]
        [InlineData("https://example.org/play/statistics/bowling-figures", StoolballRouteType.BowlingFigures)]
        [InlineData("https://example.org/play/statistics/player-performances", StoolballRouteType.PlayerPerformances)]
        [InlineData("https://example.org/play/statistics/player-of-match", StoolballRouteType.PlayerOfTheMatchAwards)]
        [InlineData("https://example.org/play/statistics/most-player-of-match", StoolballRouteType.MostPlayerOfTheMatchAwards)]
        [InlineData("https://example.org/play/statistics/most-runs", StoolballRouteType.MostRuns)]
        [InlineData("https://example.org/play/statistics/most-wickets", StoolballRouteType.MostWickets)]
        [InlineData("https://example.org/play/statistics/most-5-wickets", StoolballRouteType.MostXWickets)]
        [InlineData("https://example.org/play/statistics/most-catches", StoolballRouteType.MostCatches)]
        [InlineData("https://example.org/play/statistics/most-run-outs", StoolballRouteType.MostRunOuts)]
        [InlineData("https://example.org/play/statistics/batting-average", StoolballRouteType.BattingAverage)]
        [InlineData("https://example.org/play/statistics/batting-strike-rate", StoolballRouteType.BattingStrikeRate)]
        [InlineData("https://example.org/play/statistics/bowling-strike-rate", StoolballRouteType.BowlingStrikeRate)]
        [InlineData("https://example.org/play/statistics/bowling-average", StoolballRouteType.BowlingAverage)]
        [InlineData("https://example.org/play/statistics/economy-rate", StoolballRouteType.EconomyRate)]
        [InlineData("https://example.org/players/example-name/individual-scores", StoolballRouteType.IndividualScores)]
        [InlineData("https://example.org/players/example-name/bowling-figures", StoolballRouteType.BowlingFigures)]
        [InlineData("https://example.org/players/example-name/player-performances", StoolballRouteType.PlayerPerformances)]
        [InlineData("https://example.org/players/example-name/player-of-match", StoolballRouteType.PlayerOfTheMatchAwards)]
        [InlineData("https://example.org/players/example-name/batting", StoolballRouteType.PlayerBatting)]
        [InlineData("https://example.org/players/example-name/bowling", StoolballRouteType.PlayerBowling)]
        [InlineData("https://example.org/players/example-name/fielding", StoolballRouteType.PlayerFielding)]
        [InlineData("https://example.org/players/example-name/catches", StoolballRouteType.Catches)]
        [InlineData("https://example.org/players/example-name/run-outs", StoolballRouteType.RunOuts)]
        [InlineData("https://example.org/players/example-name/add-to-my-statistics", StoolballRouteType.LinkPlayerToMember)]
        [InlineData("https://example.org/players/example-name/", StoolballRouteType.Player)]
        [InlineData("https://example.org/schools/find/", StoolballRouteType.Schools)]
        [InlineData("https://example.org/account/my-statistics/", StoolballRouteType.LinkedPlayersForMember)]
        public void Correct_route_should_match(string route, StoolballRouteType expectedType)
        {
            var requestUrl = new Uri(route);
            var parser = new StoolballRouteParser();

            var result = parser.ParseRouteType(requestUrl.AbsolutePath.TrimEnd('/'));
            Assert.Equal(expectedType, result);

            // Assert case-insensitive match
            result = parser.ParseRouteType(requestUrl.AbsolutePath.ToUpperInvariant());
            Assert.Equal(expectedType, result);

            // Assert trailing slash is optional
            result = parser.ParseRouteType(requestUrl.AbsolutePath.TrimEnd('/') + '/');
            Assert.Equal(expectedType, result);
        }

        [Theory]
        [InlineData("https://example.org/clubs")]
        [InlineData("https://example.org/clubs/")]
        [InlineData("https://example.org/clubs/example/invalid")]
        [InlineData("https://example.org/teams/example/invalid")]
        [InlineData("https://example.org/teams/example123/teams/example-team/")]
        [InlineData("https://example.org/teams/example123/individualscores/")]
        [InlineData("https://example.org/teams/example123/individual-scores/")]
        [InlineData("https://example.org/tournaments/example123/invalid/example-team/")]
        [InlineData("https://example.org/competitions/example/2020-")]
        [InlineData("https://example.org/competitions/example/2020/invalid")]
        [InlineData("https://example.org/competitions/example/2020-21/invalid/")]
        [InlineData("https://example.org/matches/example-match/edit/innings/one/bowling")]
        [InlineData("https://example.org/matches/example-match/edit/innings/1a/bowling")]
        [InlineData("https://example.org/matches/example-match/edit/innings/1/invalid/")]
        [InlineData("https://example.org/tournaments/invalid/rss")]
        [InlineData("https://example.org/tournaments/invalid/calendar/ics")]
        [InlineData("https://example.org/schools")]
        [InlineData("https://example.org/schools/")]
        [InlineData("https://example.org/schools/invalid")]
        [InlineData("https://example.org/other")]
        [InlineData("https://example.org/other/")]
        [InlineData("https://example.org/players/example/invalid")]
        [InlineData("https://example.org/teams/example123/edit/some-player/rename")]
        [InlineData("https://example.org/teams/example123/edit/players/some-player/invalid")]
        public void Other_route_should_not_match(string route)
        {
            var requestUrl = new Uri(route);

            var result = new StoolballRouteParser().ParseRouteType(requestUrl.AbsolutePath);

            Assert.Null(result);
        }
    }
}
