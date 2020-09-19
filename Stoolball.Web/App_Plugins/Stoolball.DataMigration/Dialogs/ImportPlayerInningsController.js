(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportPlayerInnings", function (
      $http,
      $scope,
      umbRequestHelper,
      stoolballResource
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

      async function getBattingToMigrate(
        dataSource,
        apiKey,
        position,
        batchSize
      ) {
        return await umbRequestHelper.resourcePromise(
          $http.get(
            "https://" +
              dataSource +
              "/data/batting-api.php?key=" +
              apiKey +
              "&from=" +
              position +
              "&batchSize=" +
              batchSize
          ),
          "Failed to retrieve Stoolball England player performance data"
        );
      }

      async function importBatting(performances, imported, failed) {
        await stoolballResource.postManyToApi(
          "PlayerPerformanceMigration/CreatePlayerInnings",
          performances,
          (batting) => ({
            MigratedMatchId: batting.matchId,
            MigratedPlayerIdentityId: batting.playerId,
            MigratedTeamId: batting.teamId,
            MigratedMatchTeamId: batting.matchTeamId,
            BattingPosition: batting.battingPosition,
            DismissalType: batting.howOut,
            MigratedDismissedById: batting.dismissedById,
            MigratedBowlerId: batting.bowlerId,
            RunsScored: batting.runsScored,
            BallsFaced: batting.ballsFaced,
            History: [
              {
                Action: "Create",
                AuditDate: batting.dateCreated,
              },
            ],
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
            const batchSize = 100;
            let position = 0;
            let first = true;
            while (first || position <= vm.total) {
              first = false;

              let batting = await getBattingToMigrate(
                $scope.model.dataSource,
                apiKey,
                position,
                batchSize
              );

              vm.total = batting.total;

              if (batting.performances && batting.performances.length) {
                await importBatting(
                  batting.performances,
                  vm.imported,
                  vm.failed
                );
              }

              position = position + batchSize;
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
          vm.imported = null;
          vm.failed = null;
          $scope.model.close();
        }
      }
    });
})();
