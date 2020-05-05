(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportMatchLocations", function (
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

      async function getMatchLocationsToMigrate(dataSource, apiKey) {
        return await umbRequestHelper.resourcePromise(
          $http.get(
            "https://" + dataSource + "/data/grounds-api.php?key=" + apiKey
          ),
          "Failed to retrieve all Stoolball England ground data"
        );
      }
      async function importMatchLocations(locations, imported, failed) {
        await stoolballResource.postManyToApi(
          "MatchLocationMigration/CreateMatchLocation",
          locations,
          (location) => ({
            MigratedMatchLocationId: location.groundId,
            SortName: location.sortName,
            SecondaryAddressableObjectName: location.saon,
            PrimaryAddressableObjectName: location.paon,
            StreetDescription: location.street,
            Locality: location.locality,
            Town: location.town,
            AdministrativeArea: location.administrativeArea,
            Postcode: location.postcode,
            Latitude: location.latitude,
            Longitude: location.longitude,
            GeoPrecision:
              location.geoPrecision == null ? null : location.geoPrecision - 1,
            MatchLocationNotes:
              location.directions + location.parking + location.facilities,
            MatchLocationRoute: location.route,
            History: [
              {
                Action: "Create",
                AuditDate: location.dateCreated,
              },
              {
                Action: "Update",
                AuditDate: location.dateUpdated,
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
            let locations = await getMatchLocationsToMigrate(
              $scope.model.dataSource,
              apiKey
            );

            vm.total = locations.length;

            await importMatchLocations(locations, vm.imported, vm.failed);

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
