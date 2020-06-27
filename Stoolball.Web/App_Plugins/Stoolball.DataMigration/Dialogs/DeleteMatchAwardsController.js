(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.DeleteMatchAwards", function (
      $scope,
      stoolballResource
    ) {
      let vm = this;

      vm.submit = submit;
      vm.close = close;
      vm.buttonState = "init";
      vm.done = false;
      vm.success = null;

      async function deleteMatchAwards(callback) {
        return await stoolballResource.deleteApi(
          "MatchAwardMigration/DeleteMatchAwards",
          callback
        );
      }

      async function submit() {
        vm.buttonState = "busy";

        try {
          await deleteMatchAwards((result) => (vm.success = result));
          vm.done = true;
          vm.buttonState = "success";
        } catch (e) {
          vm.buttonState = "error";
          console.log(e);
        }
      }

      function close() {
        if ($scope.model.close) {
          $scope.model.close();
        }
      }
    });
})();
