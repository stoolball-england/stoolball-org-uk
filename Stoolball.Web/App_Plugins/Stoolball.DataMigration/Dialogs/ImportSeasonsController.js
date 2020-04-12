(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportSeasons", function (
      $http,
      $scope,
      stoolballResource,
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
          (season) => ({
            SeasonId: season.seasonId,
            SeasonName: season.seasonName,
            Competition: {
              CompetitionId: season.competitionId,
              CompetitionRoute: season.competitionRoute,
            },
            Teams: season.teams.map((x) => {
              return {
                Team: { TeamId: x.teamId },
                WithdrawnDate: x.withdrawnDate,
              };
            }),
            IsLatestSeason: season.isLatestSeason,
            StartYear: season.startYear,
            EndYear: season.endYear,
            Introduction: season.introduction,
            Results: season.results,
            ShowTable: season.showTable,
            ShowRunsScored: season.showRunsScored,
            ShowRunsConceded: season.showRunsConceded,
            SeasonRoute: season.route,
            DateCreated: season.dateCreated,
            DateUpdated: season.dateUpdated,
          }),
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
})();
