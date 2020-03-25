(function() {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.DeleteMembers", function(
      $scope,
      entityResource,
      stoolballResource
    ) {
      let vm = this;

      vm.submit = submit;
      vm.close = close;
      vm.totalMembers = -1;
      vm.deletedMembers = [];
      vm.buttonState = "init";
      vm.done = false;

      function submit() {
        vm.buttonState = "busy";

        entityResource.getAll("member").then(async function(members) {
          vm.totalMembers = members.length;

          try {
            await stoolballResource.deleteMembers(members, vm.deletedMembers);
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
