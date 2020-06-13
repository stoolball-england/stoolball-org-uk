"use strict";

function tournamentResource() {
  return {
    tournamentReducer(tournament) {
      tournament.teams = tournament.teams || [];

      return {
        MigratedTournamentId: tournament.matchId,
        TournamentName: tournament.title,
        MigratedTournamentLocationId: tournament.groundId,
        TournamentQualificationType:
          tournament.qualification === 0 ? null : tournament.qualification - 1,
        PlayerType: tournament.playerType - 1,
        PlayersPerTeam: tournament.playersPerTeam,
        OversPerInningsDefault: tournament.overs,
        MaximumTeamsInTournament: tournament.maximumTeamsInTournament,
        SpacesInTournament: tournament.spacesInTournament,
        StartTime: tournament.startTime,
        StartTimeIsKnown: tournament.startTimeKnown,
        MatchNotes: tournament.notes,
        MigratedTeams: tournament.teams.map((team) => ({
          MigratedTeamId: team.teamId,
          TeamRole: 2,
        })),
        MigratedSeasonIds: tournament.seasons,
        TournamentRoute: tournament.route,
        History: [
          {
            Action: "Create",
            AuditDate: tournament.dateCreated,
            ActorName: tournament.createdBy ? tournament.createdBy : null,
          },
          {
            Action: "Update",
            AuditDate: tournament.dateUpdated,
            ActorName: tournament.updatedBy ? tournament.updatedBy : null,
          },
        ],
      };
    },
  };
}

// For Jest tests
if (typeof module !== "undefined" && typeof module.exports !== "undefined") {
  module.exports = tournamentResource;
}

//adds the resource to umbraco.resources module:
if (typeof angular !== "undefined") {
  angular
    .module("umbraco.resources")
    .factory("tournamentResource", tournamentResource);

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportTournaments", function (
      $http,
      $scope,
      umbRequestHelper,
      stoolballResource,
      tournamentResource
    ) {
      let vm = this;

      vm.submit = submit;
      vm.close = close;
      vm.buttonState = "init";
      vm.processing = false;
      vm.imported = [];
      vm.failed = [];
      vm.total = "?";
      vm.done = false;

      async function getTournamentsToMigrate(dataSource, apiKey) {
        return await umbRequestHelper.resourcePromise(
          $http.get(
            "https://" +
              dataSource +
              "/data/matches-api.php?key=" +
              apiKey +
              "&type=1&from=0&batchSize=10000"
          ),
          "Failed to retrieve Stoolball England tournament data"
        );
      }

      async function importTournaments(tournaments, imported, failed) {
        await stoolballResource.postManyToApi(
          "TournamentMigration/CreateTournament",
          tournaments,
          (tournament) => tournamentResource.tournamentReducer(tournament),
          imported,
          failed
        );
      }

      function submit() {
        vm.buttonState = "busy";

        stoolballResource.getApiKey().then(async (apiKey) => {
          vm.processing = true;
          try {
            let tournaments = await getTournamentsToMigrate(
              $scope.model.dataSource,
              apiKey
            );

            vm.total = tournaments.total;

            if (tournaments.matches && tournaments.matches.length) {
              await importTournaments(
                tournaments.matches,
                vm.imported,
                vm.failed
              );
            }

            vm.done = true;
            vm.buttonState = "success";
          } catch (e) {
            vm.buttonState = "error";
            console.log(e);
          }
        });
      }

      function close() {
        if ($scope.model.close) {
          $scope.model.close();
        }
      }
    });
}
