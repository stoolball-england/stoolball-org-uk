(function() {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportSchools", function(
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

      async function getSchoolsToMigrate(dataSource, apiKey) {
        const url =
          "https://" +
          dataSource +
          "/data/clubs-api.php?type=schools&key=" +
          apiKey;
        return await umbRequestHelper.resourcePromise(
          $http.get(url),
          "Failed to retrieve all Stoolball England schools data"
        );
      }

      async function importSchools(schools, imported, failed) {
        await stoolballResource.postManyToApi(
          "SchoolMigration/CreateSchool",
          schools,
          school => ({
            SchoolId: school.clubId,
            SchoolName: school.name,
            PlaysOutdoors: school.playsOutdoors,
            PlaysIndoors: school.playsIndoors,
            Twitter: school.twitterAccount,
            Facebook: school.facebookUrl,
            Instagram: school.instagramAccount,
            HowManyPlayers: school.howManyPlayers,
            SchoolRoute: school.route,
            DateCreated: school.dateCreated,
            DateUpdated: school.dateUpdated
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
            let schools = await getSchoolsToMigrate(
              $scope.model.dataSource,
              apiKey
            );

            vm.total = schools.length;

            await importSchools(schools, vm.imported, vm.failed);

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