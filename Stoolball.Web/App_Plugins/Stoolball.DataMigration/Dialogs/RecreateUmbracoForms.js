(function () {
  "use strict";

  angular
    .module("umbraco")
    .controller(
      "Stoolball.DataMigration.RecreateUmbracoForms",
      function ($scope, stoolballResource) {
        let vm = this;

        vm.submit = submit;
        vm.close = close;
        vm.buttonState = "init";
        vm.done = false;

        async function recreateForms() {
          await stoolballResource.postToApi(
            "RecreateUmbracoFormsMigration/RecreateUmbracoForms"
          );
        }

        async function submit() {
          vm.buttonState = "busy";

          try {
            await recreateForms();

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
      }
    );
})();
