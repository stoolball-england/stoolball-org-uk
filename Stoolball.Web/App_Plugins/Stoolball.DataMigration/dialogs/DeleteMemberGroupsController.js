(function() {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.DeleteMemberGroups", function(
      $http,
      $scope,
      stoolballResource,
      umbRequestHelper
    ) {
      let vm = this;

      vm.submit = submit;
      vm.close = close;
      vm.totalGroups = -1;
      vm.deletedGroups = [];
      vm.buttonState = "init";
      vm.done = false;

      function getMemberGroups() {
        const url =
          "/umbraco/backoffice/Migration/MemberMigration/MemberGroups";
        return umbRequestHelper.resourcePromise(
          $http.get(url),
          "Failed to retrieve member groups"
        );
      }

      async function deleteMemberGroups(groups, deleted) {
        await stoolballResource.postManyToApi(
          "MemberMigration/DeleteMemberGroup",
          groups,
          group => group,
          deleted
        );
      }

      function submit() {
        vm.buttonState = "busy";

        getMemberGroups().then(async function(groups) {
          groups = groups.filter(group => group.Name !== "All Members");
          vm.totalGroups = groups.length;

          try {
            await deleteMemberGroups(groups, vm.deletedGroups);
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
