(function() {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.ImportClubs", function(
      $scope,
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

      function submit() {
        vm.buttonState = "busy";

        stoolballResource.getApiKey().then(async apiKey => {
          vm.processing = true;
          try {
            let clubs = await stoolballResource.getClubsToMigrate(
              $scope.model.dataSource,
              apiKey
            );

            vm.total = clubs.length;

            await stoolballResource.importClubs(clubs, vm.imported, vm.failed);

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
