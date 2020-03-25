(function() {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.DeleteMemberGroups", function(
      $scope,
      stoolballResource
    ) {
      let vm = this;

      vm.submit = submit;
      vm.close = close;
      vm.totalGroups = -1;
      vm.deletedGroups = [];
      vm.buttonState = "init";
      vm.done = false;

      function submit() {
        vm.buttonState = "busy";

        stoolballResource.getMemberGroups().then(async function(groups) {
          groups = groups.filter(group => group.Name !== "All Members");
          vm.totalGroups = groups.length;

          try {
            await stoolballResource.deleteMemberGroups(
              groups,
              vm.deletedGroups
            );
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
