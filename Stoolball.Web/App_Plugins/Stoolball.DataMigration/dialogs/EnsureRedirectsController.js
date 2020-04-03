(function() {
  "use strict";

  angular
    .module("umbraco")
    .controller("Stoolball.DataMigration.EnsureRedirects", function(
      $scope,
      stoolballResource
    ) {
      let vm = this;

      vm.submit = submit;
      vm.close = close;
      vm.buttonState = "init";
      vm.done = false;

      async function ensureRedirects() {
        await stoolballResource.postToApi("RedirectsMigration/EnsureRedirects");
      }

      async function submit() {
        vm.buttonState = "busy";

        try {
          await ensureRedirects();

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
