(function() {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.DeleteClubs", function(
      $scope,
      stoolballResource
    ) {
      let vm = this;

      vm.submit = submit;
      vm.close = close;
      vm.buttonState = "init";
      vm.done = false;
      vm.success = null;

      async function deleteClubs(callback) {
        return await stoolballResource.deleteApi(
          "ClubMigration/DeleteClubs",
          callback
        );
      }

      async function submit() {
        vm.buttonState = "busy";

        try {
          await deleteClubs(result => (vm.success = result));
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
