"use strict";

function seasonResource() {
  return {
    seasonReducer(season) {
      season.teams = season.teams || [];
      season.pointsRules = season.pointsRules || [];
      season.pointsAdjustments = season.pointsAdjustments || [];

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
        MatchTypes: season.matchTypes,
        PointsRules: season.pointsRules.map((x) => ({
          MatchResultType: x.resultType - (x.resultType > 3 ? 2 : 1),
          HomePoints: x.homePoints,
          AwayPoints: x.awayPoints,
        })),
        StartYear: season.startYear,
        EndYear: season.endYear,
        Introduction: season.introduction,
        Results: season.results,
        ShowTable: season.showTable,
        ShowRunsScored: season.showRunsScored,
        ShowRunsConceded: season.showRunsConceded,
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
