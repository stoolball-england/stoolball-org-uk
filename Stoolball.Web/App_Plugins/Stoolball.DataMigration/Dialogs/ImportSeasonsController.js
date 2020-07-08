"use strict";

function seasonResource() {
  return {
    seasonReducer(season) {
      season.teams = season.teams || [];
      season.pointsRules = season.pointsRules || [];
      season.pointsAdjustments = season.pointsAdjustments || [];
      season.matchTypes = season.matchTypes || [];

      const matchTypes = [];
      if (season.matchTypes.includes(0) || season.matchTypes.includes(2)) {
        matchTypes.push(0);
      }
      if (season.matchTypes.includes(3)) {
        matchTypes.push(1);
      }
      if (season.matchTypes.includes(4)) {
        matchTypes.push(2);
      }
      if (season.matchTypes.includes(5)) {
        matchTypes.push(3);
      }
      const resultTypes = { 1: 0, 2: 1, 3: 4, 5: 8, 6: 2, 7: 3, 8: 6, 9: 5 };

      return {
        MigratedSeasonId: season.seasonId,
        MigratedCompetition: {
          MigratedCompetitionId: season.competitionId,
          CompetitionRoute: season.competitionRoute,
        },
        MigratedTeams: season.teams.map((x) => {
          return {
            MigratedTeamId: x.teamId,
            WithdrawnDate: x.withdrawnDate,
          };
        }),
        MigratedPointsAdjustments: season.pointsAdjustments.map((x) => {
          return {
            MigratedTeamId: x.teamId,
            Points: x.points,
            Reason: x.reason,
          };
        }),
        MatchTypes: matchTypes,
        EnableTournaments: season.matchTypes.includes(1),
        PointsRules: season.pointsRules.map((x) => ({
          MatchResultType: resultTypes[x.resultType],
          HomePoints: x.homePoints,
          AwayPoints: x.awayPoints,
        })),
        FromYear: season.startYear,
        UntilYear: season.endYear,
        Introduction: season.introduction,
        Results: season.results,
        ResultsTableType: season.showTable ? "LeagueTable" : "None",
        EnableRunsScored: season.showRunsScored,
        EnableRunsConceded: season.showRunsConceded,
        SeasonRoute: season.route,
        History: [
          {
            Action: "Create",
            AuditDate: season.dateCreated,
          },
          {
            Action: "Update",
            AuditDate: season.dateUpdated,
          },
        ],
      };
    },
  };
}

// For Jest tests
if (typeof module !== "undefined" && typeof module.exports !== "undefined") {
  module.exports = seasonResource;
}

//adds the resource to umbraco.resources module:
if (typeof angular !== "undefined") {
  angular.module("umbraco.resources").factory("seasonResource", seasonResource);

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportSeasons", function (
      $http,
      $scope,
      stoolballResource,
      seasonResource,
      umbRequestHelper
    ) {
      let vm = this;

      vm.submit = submit;
      vm.close = close;
      vm.total = "?";
      vm.imported = [];
      vm.failed = [];
      vm.buttonState = "init";
      vm.processing = false;
      vm.done = false;

      async function getCompetitionsToMigrate(dataSource, apiKey) {
        const url =
          "https://" + dataSource + "/data/competitions-api.php?key=" + apiKey;
        return await umbRequestHelper.resourcePromise(
          $http.get(url),
          "Failed to retrieve all Stoolball England competition data"
        );
      }

      async function importSeasons(seasons, imported, failed) {
        await stoolballResource.postManyToApi(
          "CompetitionMigration/CreateSeason",
          seasons,
          (season) => seasonResource.seasonReducer(season),
          imported,
          failed
        );
      }

      function submit() {
        vm.buttonState = "busy";

        stoolballResource.getApiKey().then(async (apiKey) => {
          vm.processing = true;
          try {
            const competitions = await getCompetitionsToMigrate(
              $scope.model.dataSource,
              apiKey
            );

            const seasons = competitions.flatMap((x) => {
              return x.seasons.map((y) => {
                return {
                  ...y,
                  name: x.name + " " + y.name,
                  seasonName: y.name,
                  competitionId: x.competitionId,
                  competitionRoute: x.route,
                };
              });
            });

            vm.total = seasons.length;

            await importSeasons(seasons, vm.imported, vm.failed);

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
