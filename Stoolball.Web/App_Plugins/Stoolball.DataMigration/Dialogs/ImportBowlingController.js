(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportBowling", function (
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

      async function getBowlingToMigrate(
        dataSource,
        apiKey,
        position,
        batchSize
      ) {
        return await umbRequestHelper.resourcePromise(
          $http.get(
            "https://" +
              dataSource +
              "/data/bowling-api.php?key=" +
              apiKey +
              "&from=" +
              position +
              "&batchSize=" +
              batchSize
          ),
          "Failed to retrieve Stoolball England player performance data"
        );
      }

      async function importBowling(performances, imported, failed) {
        await stoolballResource.postManyToApi(
          "PlayerPerformanceMigration/CreateBowling",
          performances,
          (bowling) => ({
            MigratedMatchId: bowling.matchId,
            MigratedPlayerIdentityId: bowling.playerId,
            MigratedTeamId: bowling.teamId,
            OverNumber: bowling.overNumber,
            BallsBowled: bowling.ballsBowled,
            NoBalls: bowling.noBalls,
            Wides: bowling.wides,
            RunsConceded: bowling.runsConceded,
            History: [
              {
                Action: "Create",
                AuditDate: bowling.dateCreated,
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
            const batchSize = 50;
            let position = $scope.model.startFrom;
            let target = position + 10000;
            let first = true;
            while (first || (position < target && position <= vm.total)) {
              first = false;

              let bowling = await getBowlingToMigrate(
                $scope.model.dataSource,
                apiKey,
                position,
                batchSize
              );

              vm.total = bowling.total;

              if (bowling.performances && bowling.performances.length) {
                await importBowling(
                  bowling.performances,
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
