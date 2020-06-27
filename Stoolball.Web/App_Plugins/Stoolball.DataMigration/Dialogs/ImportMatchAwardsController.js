(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportMatchAwards", function (
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

      async function getMatchAwardsToMigrate(dataSource, apiKey) {
        return await umbRequestHelper.resourcePromise(
          $http.get(
            "https://" + dataSource + "/data/awards-api.php?key=" + apiKey
          ),
          "Failed to retrieve all Stoolball England match awards data"
        );
      }
      async function importMatchAwards(awards, imported, failed) {
        await stoolballResource.postManyToApi(
          "MatchAwardMigration/CreateMatchAward",
          awards,
          (award) => ({
            MigratedMatchId: award.matchId,
            PlayerOfTheMatchId: award.player_of_match_id,
            PlayerOfTheMatchHomeId: award.player_of_match_home_id,
            PlayerOfTheMatchAwayId: award.player_of_match_away_id,
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
            let awards = await getMatchAwardsToMigrate(
              $scope.model.dataSource,
              apiKey
            );

            vm.total = awards.length;

            await importMatchAwards(awards, vm.imported, vm.failed);

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
