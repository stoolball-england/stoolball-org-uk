(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportPlayers", function (
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

      async function getPlayersToMigrate(
        dataSource,
        apiKey,
        position,
        batchSize
      ) {
        return await umbRequestHelper.resourcePromise(
          $http.get(
            "https://" +
              dataSource +
              "/data/players-api.php?key=" +
              apiKey +
              "&from=" +
              position +
              "&batchSize=" +
              batchSize
          ),
          "Failed to retrieve Stoolball England player data"
        );
      }

      async function importPlayers(players, imported, failed) {
        await stoolballResource.postManyToApi(
          "PlayerMigration/CreatePlayer",
          players,
          (player) => ({
            MigratedPlayerIdentityId: player.playerId,
            PlayerIdentityName: player.name,
            MigratedTeamId: player.teamId,
            PlayerIdentityRoute: player.route,
            History: [
              {
                Action: "Create",
                AuditDate: player.dateCreated,
              },
              {
                Action: "Update",
                AuditDate: player.dateUpdated,
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

              let players = await getPlayersToMigrate(
                $scope.model.dataSource,
                apiKey,
                position,
                batchSize
              );

              vm.total = players.total;

              if (players.players && players.players.length) {
                await importPlayers(players.players, vm.imported, vm.failed);
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
          $scope.model.close();
        }
      }
    });
})();
