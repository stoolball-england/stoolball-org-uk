(function() {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportClubs", function(
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

      async function getClubsToMigrate(dataSource, apiKey) {
        const url =
          "https://" + dataSource + "/data/clubs-api.php?key=" + apiKey;
        return await umbRequestHelper.resourcePromise(
          $http.get(url),
          "Failed to retrieve all Stoolball England club data"
        );
      }

      async function importClubs(clubs, imported, failed) {
        await stoolballResource.postManyToApi(
          "ClubMigration/CreateClub",
          clubs,
          club => ({
            ClubId: club.clubId,
            ClubName: club.name,
            PlaysOutdoors: club.playsOutdoors,
            PlaysIndoors: club.playsIndoors,
            Twitter: club.twitterAccount,
            Facebook: club.facebookUrl,
            Instagram: club.instagramAccount,
            ClubMark: club.clubmarkAccredited,
            HowManyPlayers: club.howManyPlayers,
            ClubRoute: club.route,
            DateCreated: club.dateCreated,
            DateUpdated: club.dateUpdated
          }),
          imported,
          failed
        );
      }

      function submit() {
        vm.buttonState = "busy";

        stoolballResource.getApiKey().then(async apiKey => {
          vm.processing = true;
          try {
            let clubs = await getClubsToMigrate(
              $scope.model.dataSource,
              apiKey
            );

            vm.total = clubs.length;

            await importClubs(clubs, vm.imported, vm.failed);

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
